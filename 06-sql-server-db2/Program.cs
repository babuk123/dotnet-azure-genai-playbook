// Data-access patterns with ADO.NET (works with SQL Server via Microsoft.Data.SqlClient, DB2 via IBM.Data.Db2).
// Shown as reference — needs a database to run.
using System.Data.Common;

public static class StatementRepository
{
    // Always parameterized: prevents SQL injection and lets the server reuse plans
    public static async Task<List<(DateTime Date, decimal Amount, decimal Balance)>> GetStatementAsync(
        DbConnection conn, string accountNo, DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.usp_GetStatement";
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        AddParam(cmd, "@AccountNo", accountNo);
        AddParam(cmd, "@From", from.ToDateTime(TimeOnly.MinValue));
        AddParam(cmd, "@To", to.ToDateTime(TimeOnly.MinValue));

        var rows = new List<(DateTime, decimal, decimal)>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            rows.Add((reader.GetDateTime(0), reader.GetDecimal(2), reader.GetDecimal(3)));
        return rows;
    }

    static void AddParam(DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter(); p.ParameterName = name; p.Value = value; cmd.Parameters.Add(p);
    }
}
