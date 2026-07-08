using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Emailing;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.EmailTemplates;
using HrmSystem.Domain.Entities.EmailTemplates.ValueObjects;
using HrmSystem.Domain.Entities.Users;

namespace HrmSystem.Application.Features.Users.ForgotPassword;

internal sealed class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityService _identityService;
    private readonly IClientUrlProvider _clientUrlProvider;
    private readonly IEmailTemplateRepository _emailTemplateRepository;
    private readonly ITemplateRenderer _renderer;
    private readonly IEmailService _emailService;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IIdentityService identityService,
        IClientUrlProvider clientUrlProvider,
        IEmailTemplateRepository emailTemplateRepository,
        ITemplateRenderer renderer,
        IEmailService emailService
    )
    {
        _userRepository = userRepository;
        _identityService = identityService;
        _clientUrlProvider = clientUrlProvider;
        _emailTemplateRepository = emailTemplateRepository;
        _renderer = renderer;
        _emailService = emailService;
    }

    public async Task<Result> Handle(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken
    )
    {
        /*
            //!     ALWAYS return success — never reveal whether the email is registered.
            //!     If no user is found, return immediately without sending an email.
            //!     The caller receives the same 200 OK either way.
        */
        User? user = await _userRepository.FindByAnyIdentifierAsync(
            command.Email,
            cancellationToken
        );

        if (user is null)
        {
            return Result.Success();
        }

        /*
            //?     Token is generated via ASP.NET Data Protection — time-limited and
            //?     purpose-scoped to "password reset". Not stored in the database.
        */
        string passwordResetToken = await _identityService.GeneratePasswordResetTokenAsync(
            user.IdentityId,
            cancellationToken
        );

        Uri resetUri = _clientUrlProvider.BuildPasswordResetUrl(
            user.Email.Value,
            passwordResetToken
        );

        Result<EmailTemplate> templateResult = await _emailTemplateRepository.FindByKeyAsync(
            EmailTemplateKey.UserPasswordReset,
            cancellationToken
        );

        if (templateResult.IsFailure)
        {
            //! Template row is missing from the database — email cannot be sent.
            //! Return success anyway (enumeration protection) but this should be alerted in prod.
            return Result.Success();
        }

        EmailTemplate template = templateResult.Value;

        var variables = new Dictionary<string, string>
        {
            ["UserName"] = user.UserName.Value,
            ["ResetUrl"] = resetUri.AbsoluteUri,
        };

        string subject = _renderer.Render(template.Subject.Value, variables);
        string body = _renderer.Render(template.Body.Value, variables);

        await _emailService.SendAsync(user.Email, subject, body, cancellationToken);

        return Result.Success();
    }
}
