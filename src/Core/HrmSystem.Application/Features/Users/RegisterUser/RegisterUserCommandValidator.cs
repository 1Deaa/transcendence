using FluentValidation;
using HrmSystem.Application.Common.Validation;

namespace HrmSystem.Application.Features.Users.RegisterUser;

//TODO: Add Messages professionally with Domain Errors (Messages + Descriptions) //! As examples below:
/*
     public ChangeHabitStatusesCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithErrorCode(ApplicationErrors.HabitIdIsRequiredForUpdating.Code)
            .WithMessage(ApplicationErrors.HabitIdIsRequiredForUpdating.Description)
    }

 */
internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(c => c.UserName).NotEmpty();
        RuleFor(c => c.FirstName).NotEmpty();
        RuleFor(c => c.LastName).NotEmpty();
        RuleFor(c => c.Email).NotEmpty().StrictEmailAddress();
        RuleFor(c => c.SecondaryEmail).StrictEmailAddress().When(c => c.SecondaryEmail is not null);

        //! Password validation here is a fast pre-check before any DB call.
        //! Identity will enforce its own policy rules on top of these.
        RuleFor(c => c.Password).NotEmpty().StrongPassword();

        //! [ConfirmPassword] cross-field check — lives here so all command validation is in one place.
        RuleFor(c => c.ConfirmPassword)
            .Equal(c => c.Password)
            .WithMessage("Password and confirmation password do not match.");

        //! Find a way to check phone numbers
        //? Check this method below in Domain; I can do the same:
        /*
                //if (!new PhoneAttribute().IsValid(trimmed))
                //{
                //    errors.Add(UserErrors.PhoneNumber.InvalidFormat);
                //}
        */
    }
}
