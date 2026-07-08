using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Users.RefreshTokens.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Users;

internal sealed class RefreshTokenIdConverter : ValueConverter<RefreshTokenId, string>
{
    public RefreshTokenIdConverter()
        : base(
            id => id.Value,
            rawStr => ConvertToRefreshTokenId(rawStr)
        )
    { }

    private static RefreshTokenId ConvertToRefreshTokenId(string rawStr)
    {
        Result<RefreshTokenId> result = RefreshTokenId.From(rawStr);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Database contains a corrupted RefreshTokenId: '{rawStr}'."
                    + $" (Error: {result.FirstError.Description})"
            );
        }

        return result.Value;
    }
}
