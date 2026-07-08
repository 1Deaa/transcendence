using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Domain.Entities.Imports.ValueObjects;

/*
    //?     Strongly-typed identity of an ImportJob.
    //?     Format: "import-{UUIDv7}" — New() always valid; From() validates untrusted input.
*/
public sealed record ImportJobId
{
    #region Constants

    public const string Prefix = "import-";

    #endregion

    #region Properties

    public string Value { get; }

    #endregion

    #region Constructor

    private ImportJobId(string value)
    {
        Value = value;
    }

    #endregion

    #region Factories

    public static ImportJobId New() =>
        new(string.Concat(Prefix, Guid.CreateVersion7().ToString()));

    public static Result<ImportJobId> From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ImportJobErrors.Id.Invalid;
        }

        if (!value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return ImportJobErrors.Id.InvalidFormat;
        }

        return new ImportJobId(value);
    }

    #endregion

    #region Overrides

    public override string ToString() => Value;

    #endregion
}
