using HrmSystem.Application.Common.Interfaces.Emailing;
using HrmSystem.Domain.Entities.Users.ValueObjects;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace HrmSystem.Infrastructure.Services.Emailing;

/*
    //*     Delivers emails via SMTP using MailKit — the library Microsoft recommends
    //*     as a replacement for the obsolete [System.Net.Mail.SmtpClient].
    //
    //>         Step 1 — enable User Secrets on the Web project (one time):
    //>         ! dotnet user-secrets init --project src/Web/HrmSystem.Web
      >
    //>         Step 2 — store your Gmail credentials:
    //>         ! dotnet user-secrets set "SmtpSettings:Username" "your@gmail.com" --project src/Web/HrmSystem.Web
    //>         ! dotnet user-secrets set "SmtpSettings:Password" "abcd efgh ijkl mnop" --project src/Web/HrmSystem.Web
      >
    //>         Step 3 — get the App Password from Google:
    //>         1. Go to myaccount.google.com
    //>         2. Security → 2-Step Verification (must be ON)
    //>         3. Security → App passwords → create one → copy the 16-char code
    //>         4. Paste it in the user-secrets set Password command above
    //
    //!     [StartTls] (port 587) upgrades a plain connection to encrypted mid-handshake.
    //!     Do NOT use [SslOnConnect] (port 465) unless your SMTP host requires it — they
    //!     are different protocols. Gmail 587 + StartTls is the correct combination.
*/
internal sealed class EmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;

    public EmailService(IOptions<SmtpSettings> options)
    {
        _smtpSettings = options.Value;
    }

    public async Task SendAsync(Email to, string subject, string body, CancellationToken ct)
    {
        /*
            //?     MimeMessage is MailKit's representation of an RFC 2822 email message.
            //?     BodyBuilder handles the MIME multipart structure — plain-text fallback
            //?     is omitted here since templates are HTML only.
        */
        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromEmail));
        message.To.Add(new MailboxAddress(string.Empty, to.Value));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = body }.ToMessageBody();

        /*
            //*     [SmtpClient] from MailKit (not [System.Net.Mail.SmtpClient]).
            //*     The [using] block ensures [DisconnectAsync] is called even on exceptions
            //*     so the underlying TCP connection is properly released.
        */
        using var smtp = new SmtpClient();

        await smtp.ConnectAsync(
            _smtpSettings.Host,
            _smtpSettings.Port,
            SecureSocketOptions.StartTls,
            ct
        );
        await smtp.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password, ct);
        await smtp.SendAsync(message, ct);
        await smtp.DisconnectAsync(quit: true, ct);
    }
}
