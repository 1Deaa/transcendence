using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Users.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Users;

internal sealed class UserIdConverter : ValueConverter<UserId, string>
{
    public UserIdConverter()
        : base(
            userId => userId.Value, //? Convert UserId to string for storage
            rawStr => ConvertToUserId(rawStr)
        )
    { }

    private static UserId ConvertToUserId(string rawStr)
    {
        Result<UserId> result = UserId.From(rawStr);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Database contains a corrupted UserId: '{rawStr}'."
                    + $" (Error: {result.FirstError.Description})"
            );
        }

        return result.Value;
    }
}
