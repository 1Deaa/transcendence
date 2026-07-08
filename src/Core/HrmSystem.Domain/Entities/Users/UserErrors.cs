using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.Users;

public static class UserErrors
{
    // ── Aggregate-level ──────────────────────────────────────────────────────

    public static readonly Error NotFound = Error.NotFound(
        "User.NotFound",
        "The user with the specified identifier was not found."
    );

    /*
        //!     Same error for both "user not found" and "wrong password" — prevents user enumeration.
        //!     An attacker must not be able to tell whether an email exists in the system.
    */
    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "User.InvalidCredentials",
        "The identifier or password is incorrect."
    );

    public static readonly Error EmailAlreadyInUse = Error.Conflict(
        "User.EmailAlreadyInUse",
        "A user with this email address already exists."
    );

    public static readonly Error UsernameAlreadyTaken = Error.Conflict(
        "User.UsernameAlreadyTaken",
        "This username is already taken."
    );

    /*
        //!     Used by password-reset and email-confirm flows when the submitted token is
        //!     invalid, expired, or was issued for a different user.
        //!     Intentionally generic — never reveal WHY the token failed (enumeration risk).
    */
    public static readonly Error InvalidToken = Error.Validation(
        "User.InvalidToken",
        "The token is invalid or has expired."
    );

    /*
        //!     Returned when a handler requires an authenticated user but [ICurrentUserContext]
        //!     reports no valid principal. Should never be reached under normal [Authorize]
        //!     routing — treat it as a safety net for edge cases (token corruption, etc.).
    */
    public static readonly Error NotAuthenticated = Error.Unauthorized(
        "User.NotAuthenticated",
        "Authentication is required to access this resource."
    );

    // ── UserId value object ──────────────────────────────────────────────────

    public static class Id
    {
        public static readonly Error Invalid = Error.Validation(
            "User.Id.Invalid",
            "The user identifier is required and cannot be empty."
        );

        public static readonly Error InvalidFormat = Error.Validation(
            "User.Id.InvalidFormat",
            "The user identifier format is invalid. Expected format: \"user-{id}\"."
        );
    }

    // ── UserName value object ────────────────────────────────────────────────

    public static class UserName
    {
        public static readonly Error Required = Error.Validation(
            "User.UserName.Required",
            "Username is required and cannot be empty."
        );

        public static readonly Error TooLong = Error.Validation(
            "User.UserName.TooLong",
            "Username cannot exceed 50 characters."
        );

        public static readonly Error InvalidCharacters = Error.Validation(
            "User.UserName.InvalidCharacters",
            "Username may only contain letters, digits, underscores, and hyphens."
        );
    }

    // ── FirstName value object ───────────────────────────────────────────────

    public static class FirstName
    {
        public static readonly Error Required = Error.Validation(
            "User.FirstName.Required",
            "First name is required and cannot be empty."
        );

        public static readonly Error TooLong = Error.Validation(
            "User.FirstName.TooLong",
            "First name cannot exceed 100 characters."
        );
    }

    // ── MiddleName value object ──────────────────────────────────────────────

    public static class MiddleName
    {
        public static readonly Error TooLong = Error.Validation(
            "User.MiddleName.TooLong",
            "Middle name cannot exceed 100 characters."
        );
    }

    // ── LastName value object ────────────────────────────────────────────────

    public static class LastName
    {
        public static readonly Error Required = Error.Validation(
            "User.LastName.Required",
            "Last name is required and cannot be empty."
        );

        public static readonly Error TooLong = Error.Validation(
            "User.LastName.TooLong",
            "Last name cannot exceed 100 characters."
        );
    }

    // ── Email value object ───────────────────────────────────────────────────

    public static class Email
    {
        public static readonly Error Required = Error.Validation(
            "User.Email.Required",
            "Email address is required and cannot be empty."
        );

        public static readonly Error InvalidFormat = Error.Validation(
            "User.Email.InvalidFormat",
            "The email address format is invalid."
        );

        public static readonly Error TooLong = Error.Validation(
            "User.Email.TooLong",
            "Email address cannot exceed 254 characters."
        );
    }

    // ── SecondaryEmail value object ──────────────────────────────────────────

    public static class SecondaryEmail
    {
        public static readonly Error InvalidFormat = Error.Validation(
            "User.SecondaryEmail.InvalidFormat",
            "The secondary email address format is invalid."
        );

        public static readonly Error TooLong = Error.Validation(
            "User.SecondaryEmail.TooLong",
            "Secondary email address cannot exceed 254 characters."
        );

        public static readonly Error SameAsPrimary = Error.Conflict(
            "User.SecondaryEmail.SameAsPrimary",
            "Secondary email must be different from the primary email address."
        );
    }

    // ── PhoneNumber value object ─────────────────────────────────────────────

    public static class PhoneNumber
    {
        public static readonly Error InvalidFormat = Error.Validation(
            "User.PhoneNumber.InvalidFormat",
            "Phone number format is invalid. Expected E.164 format (e.g. \"+1234567890\")."
        );

        public static readonly Error TooLong = Error.Validation(
            "User.PhoneNumber.TooLong",
            "Phone number cannot exceed 20 characters."
        );
    }

    // ── SecondaryPhoneNumber value object ────────────────────────────────────

    public static class SecondaryPhoneNumber
    {
        public static readonly Error InvalidFormat = Error.Validation(
            "User.SecondaryPhoneNumber.InvalidFormat",
            "Secondary phone number format is invalid. Expected E.164 format (e.g. \"+1234567890\")."
        );

        public static readonly Error TooLong = Error.Validation(
            "User.SecondaryPhoneNumber.TooLong",
            "Secondary phone number cannot exceed 20 characters."
        );

        public static readonly Error SameAsPrimary = Error.Conflict(
            "User.SecondaryPhoneNumber.SameAsPrimary",
            "Secondary phone number must be different from the primary phone number."
        );
    }
}
