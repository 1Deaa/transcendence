namespace HrmSystem.Web.Api.Controllers.Users;

/// <summary>
/// DTO for updating user
/// </summary>
public sealed class UpdateProfileRequest
{
    /// <summary>
    /// Gets the name associated with the current instance.
    /// </summary>
    public string Name { get; init; } = string.Empty;
}
