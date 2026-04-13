using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BOTC.Infrastructure.Errors;

internal static class DatabaseExceptionClassifier
{
    private const int SqliteConstraintErrorCode = 19;
    private const int SqliteConstraintUniqueExtendedErrorCode = 2067;
    private const int SqliteConstraintForeignKeyExtendedErrorCode = 787;
    private const string PostgresUniqueViolationSqlState = PostgresErrorCodes.UniqueViolation;
    private const string PostgresForeignKeyViolationSqlState = PostgresErrorCodes.ForeignKeyViolation;

    public static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.InnerException switch
        {
            PostgresException postgresException => postgresException.SqlState == PostgresUniqueViolationSqlState,
            not null when IsSqliteConstraintViolation(
                exception.InnerException,
                SqliteConstraintErrorCode,
                SqliteConstraintUniqueExtendedErrorCode) => true,
            _ => false
        };
    }

    public static bool IsForeignKeyConstraintViolation(DbUpdateException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.InnerException switch
        {
            PostgresException postgresException => postgresException.SqlState == PostgresForeignKeyViolationSqlState,
            not null when IsSqliteConstraintViolation(
                exception.InnerException,
                SqliteConstraintErrorCode,
                SqliteConstraintForeignKeyExtendedErrorCode) => true,
            _ => false
        };
    }

    private static bool IsSqliteConstraintViolation(Exception exception, int errorCode, int extendedErrorCode)
    {
        var exceptionType = exception.GetType();

        if (!string.Equals(exceptionType.FullName, "Microsoft.Data.Sqlite.SqliteException", StringComparison.Ordinal))
        {
            return false;
        }

        var sqliteErrorCode = exceptionType.GetProperty("SqliteErrorCode")?.GetValue(exception) as int?;
        var sqliteExtendedErrorCode = exceptionType.GetProperty("SqliteExtendedErrorCode")?.GetValue(exception) as int?;

        return sqliteErrorCode == errorCode
               && sqliteExtendedErrorCode == extendedErrorCode;
    }
}