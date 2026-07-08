using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.EmailTemplates;
using HrmSystem.Domain.Entities.EmailTemplates.ValueObjects;
using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace HrmSystem.Infrastructure.Persistence.Repositories;

internal sealed class EmailTemplateRepository
    : ABaseRepository<EmailTemplate, EmailTemplateId>,
        IEmailTemplateRepository
{
    public EmailTemplateRepository(ApplicationDbContext applicationDbContext)
        : base(applicationDbContext) { }

    public async Task<Result<EmailTemplate>> FindByKeyAsync(
        EmailTemplateKey key,
        CancellationToken ct
    )
    {
        EmailTemplate? template = await DbContext
            .Set<EmailTemplate>()
            .FirstOrDefaultAsync(e => e.Key == key, ct);

        if (template is null)
        {
            return EmailTemplateErrors.NotFound;
        }

        return template;
    }
}
