using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.EmailTemplates;
using HrmSystem.Domain.Entities.EmailTemplates.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.Data.Repositories;

public interface IEmailTemplateRepository : IBaseRepository<EmailTemplate, EmailTemplateId>
{
    //? Loads the template for the given key, or returns EmailTemplateErrors.NotFound.
    //? Prefer this over GetByIdAsync — templates are always resolved by semantic key, not raw ID.
    Task<Result<EmailTemplate>> FindByKeyAsync(EmailTemplateKey key, CancellationToken ct);
}
