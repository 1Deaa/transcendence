using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Application.Common.Errors;

public static class ApplicationErrors
{
    public static Error HabitIdIsRequiredForUpdating =>
        Error.Validation(
            "HabitId.HabitIdIsRequiredForUpdating",
            "Habit Id is required for updating."
        );

    public static Error InvalidHabitStatus =>
        Error.Validation(
            "HabitStatus.Invalid",
            "Invalid habit status. Valid values are: Ongoing,Completed."
        );

    #region To Be Used


    public static Error InvoiceNotFound =>
        Error.NotFound("ApplicationErrors.Invoice.NotFound", "Invoice does not exist.");

    public static Error InvalidRefreshToken =>
        Error.Validation("RefreshToken.Expiry.Invalid", "Expiry must be in the future.");

    public static readonly Error ExpiredAccessTokenInvalid = Error.Conflict(
        code: "Auth.ExpiredAccessToken.Invalid",
        description: "Expired access token is not valid."
    );

    public static readonly Error UserIdClaimInvalid = Error.Conflict(
        code: "Auth.UserIdClaim.Invalid",
        description: "Invalid userId claim."
    );

    public static readonly Error RefreshTokenExpired = Error.Conflict(
        code: "Auth.RefreshToken.Expired",
        description: "Refresh token is invalid or has expired."
    );

    public static readonly Error UserNotFound = Error.NotFound(
        code: "Auth.User.NotFound",
        description: "User not found."
    );

    public static readonly Error TokenGenerationFailed = Error.Failure(
        code: "Auth.TokenGeneration.Failed",
        description: "Failed to generate new JWT token."
    );

    #endregion To Be Used
}
