using System.Text.RegularExpressions;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.Users.ValueObjects;

//? Validated username — letters, digits, underscores, and hyphens only. Max 50 characters.
public sealed record UserName
{
    #region Constants & readonly statics

    public const int MaxLength = 50;

    /*
     *? ValidPattern breakdown:
     *?   ^          : start of string
     *?   [a-zA-Z0-9_-]+ : one or more of: letters, digits, underscore, hyphen
     *?   $          : end of string
     *> Paired ^ and $ force the ENTIRE string to match, not just a portion.
     */
    private static readonly Regex ValidPattern = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

    #endregion Constants & readonly statics


    #region Properties

    public string Value { get; }

    #endregion Properties


    #region Constructor

    private UserName(string value) => Value = value;

    #endregion


    #region Factory

    //? Creates a validated UserName. Collects all broken rules before returning so the caller
    //? receives the complete error list at once.
    public static Result<UserName> Create(string? value)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(UserErrors.UserName.Required);
            return errors;
        }

        string trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            errors.Add(UserErrors.UserName.TooLong);
        }

        if (!ValidPattern.IsMatch(trimmed))
        {
            errors.Add(UserErrors.UserName.InvalidCharacters);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new UserName(trimmed);
    }

    #endregion Factory
}
