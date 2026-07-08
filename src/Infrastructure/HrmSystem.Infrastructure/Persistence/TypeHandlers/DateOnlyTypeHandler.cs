using System.Data;
using Dapper;

namespace HrmSystem.Infrastructure.Persistence.TypeHandlers;

/*
    //?     Dapper does not natively bind DateOnly against Microsoft.Data.SqlClient —
    //?     passing one throws "The member X of type System.DateOnly cannot be used as a
    //?     parameter value". This handler maps DateOnly ⇄ SQL [date] both directions.
    //!     Registered ONCE at startup (AddInfrastructure) via SqlMapper.AddTypeHandler —
    //!     Dapper type handlers are process-global statics.
*/
internal sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value) =>
        value switch
        {
            DateOnly dateOnly => dateOnly,
            DateTime dateTime => DateOnly.FromDateTime(dateTime),
            _ => DateOnly.Parse(value.ToString()!, System.Globalization.CultureInfo.InvariantCulture),
        };
}
