using HrmSystem.Domain.Common.Abstractions;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;
using HrmSystem.Domain.Entities.Users.RefreshTokens;
using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Domain.Entities.Users;

/*
 *? The User aggregate root.
 *
 *? Base class: AFullAuditableEntity<UserId>
 *?   - AAuditableEntity<UserId>  → CreatedAt/By, ModifiedAt/By
 *?   - ISoftDeletable            → IsDeleted (private setter) / MarkAsDeleted()
 *?   - IActivatable              → IsActive (private setter) / Activate() / Deactivate()
 *
 *> All state and behaviour for soft-delete and activation live in AFullAuditableEntity<TId>.
 *> An account starts active (IsActive = true) — set by the base class parameterized constructor.
 */
public class User : AFullAuditableEntity<UserId>
{
    private readonly List<RefreshToken> _refreshTokens = [];

    #region Constructor

    private User(
        UserId id,
        UserName userName,
        FirstName firstName,
        MiddleName? middleName,
        LastName lastName,
        Email email,
        PhoneNumber? phoneNumber,
        SecondaryEmail? secondaryEmail,
        SecondaryPhoneNumber? secondaryPhoneNumber
    )
        : base(id)
    {
        UserName = userName;
        FirstName = firstName;
        MiddleName = middleName;
        LastName = lastName;
        Email = email;
        PhoneNumber = phoneNumber;
        SecondaryEmail = secondaryEmail;
        SecondaryPhoneNumber = secondaryPhoneNumber;
    }

    //! Private Parameterless Constructor: for EF Core materialisation only — never use in domain or application code.
    private User()
        : base() { }

    #endregion Constructor

    #region Properties

    public UserName UserName { get; private set; } = null!;
    public FirstName FirstName { get; private set; } = null!;
    public MiddleName? MiddleName { get; private set; }
    public LastName LastName { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public PhoneNumber? PhoneNumber { get; private set; }
    public SecondaryEmail? SecondaryEmail { get; private set; }
    public SecondaryPhoneNumber? SecondaryPhoneNumber { get; private set; }

    #endregion

    #region Static factory

    /*
     *? The only way to create a valid User. Validates all inputs in a single pass — every broken
     *?  rule is collected so the caller receives the complete error list at once.
     *
     *? Aggregate-level cross-field rules validated here:
     *?   - Secondary email must differ from the primary email.
     *?   - Secondary phone number must differ from the primary phone number.
     */
    public static Result<User> Create(
        string userName,
        string firstName,
        string? middleName,
        string lastName,
        string email,
        string? phoneNumber = null,
        string? secondaryEmail = null,
        string? secondaryPhoneNumber = null
    )
    {
        var errors = new List<Error>();

        Result<UserName> userNameResult = UserName.Create(userName);
        if (userNameResult.IsFailure)
        {
            errors.AddRange(userNameResult.Errors);
        }

        Result<FirstName> firstNameResult = FirstName.Create(firstName);
        if (firstNameResult.IsFailure)
        {
            errors.AddRange(firstNameResult.Errors);
        }

        Result<MiddleName>? middleNameResult = null;
        if (middleName is not null)
        {
            middleNameResult = MiddleName.Create(middleName);
            if (middleNameResult.IsFailure)
            {
                errors.AddRange(middleNameResult.Errors);
            }
        }

        Result<LastName> lastNameResult = LastName.Create(lastName);
        if (lastNameResult.IsFailure)
        {
            errors.AddRange(lastNameResult.Errors);
        }

        Result<Email> emailResult = Email.Create(email);
        if (emailResult.IsFailure)
        {
            errors.AddRange(emailResult.Errors);
        }

        Result<PhoneNumber>? phoneNumberResult = null;
        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            phoneNumberResult = PhoneNumber.Create(phoneNumber);
            if (phoneNumberResult.IsFailure)
            {
                errors.AddRange(phoneNumberResult.Errors);
            }
        }

        Result<SecondaryEmail>? secondaryEmailResult = null;
        if (!string.IsNullOrWhiteSpace(secondaryEmail))
        {
            secondaryEmailResult = SecondaryEmail.Create(secondaryEmail);
            if (secondaryEmailResult.IsFailure)
            {
                errors.AddRange(secondaryEmailResult.Errors);
            }
        }

        Result<SecondaryPhoneNumber>? secondaryPhoneNumberResult = null;
        if (!string.IsNullOrWhiteSpace(secondaryPhoneNumber))
        {
            secondaryPhoneNumberResult = SecondaryPhoneNumber.Create(secondaryPhoneNumber);
            if (secondaryPhoneNumberResult.IsFailure)
            {
                errors.AddRange(secondaryPhoneNumberResult.Errors);
            }
        }

        //> Cross-field rules — only checked when the individual values are both valid.
        if (
            !emailResult.IsFailure
            && secondaryEmailResult is { IsFailure: false }
            && secondaryEmailResult.Value.Value == emailResult.Value.Value
        )
        {
            errors.Add(UserErrors.SecondaryEmail.SameAsPrimary);
        }

        if (
            phoneNumberResult is { IsFailure: false }
            && secondaryPhoneNumberResult is { IsFailure: false }
            && secondaryPhoneNumberResult.Value.Value == phoneNumberResult.Value.Value
        )
        {
            errors.Add(UserErrors.SecondaryPhoneNumber.SameAsPrimary);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        var id = UserId.New();

        var user = new User(
            id,
            userNameResult.Value,
            firstNameResult.Value,
            middleNameResult?.Value,
            lastNameResult.Value,
            emailResult.Value,
            phoneNumberResult?.Value,
            secondaryEmailResult?.Value,
            secondaryPhoneNumberResult?.Value
        );

        return user;
    }

    #endregion Static factory

    #region Navigation & Relations


    public IReadOnlyList<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public string IdentityId { get; private set; } = string.Empty;

    /*
        //?     The company workspace this user belongs to — stamped into the JWT as "tenant_id".
        //!     Nullable ON PURPOSE: host-level platform admins have NO tenant, so their tokens
        //!     carry no tenant claim and the tenant query filter denies them tenant data by default.
        //!     User is HOST-LEVEL (not ATenantEntity) — membership is data, not row ownership.
    */
    public TenantId? TenantId { get; private set; }

    #endregion


    #region Methods

    public void SetIdentityId(string identityId)
    {
        IdentityId = identityId;
    }

    //? Assigns the user to a company workspace (set at registration/invite time).
    public void AssignToTenant(TenantId tenantId)
    {
        TenantId = tenantId;
    }

    #endregion Methods
}
