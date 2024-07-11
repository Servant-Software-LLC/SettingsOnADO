using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace SettingsOnADO.Tests;

public class SchemaManagerForSqlite : SchemaManager
{
    public SchemaManagerForSqlite(DbConnection connection, bool shouldCloseConnection = true)
        : base(connection, shouldCloseConnection)
    {
    }

    protected override bool TableExists(string tableName)
    {
        if (connection is SqliteConnection)
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@tableName";
                DbParameter param = command.CreateParameter();
                param.ParameterName = "@tableName";
                param.Value = tableName;
                command.Parameters.Add(param);

                object? result = command.ExecuteScalar();
                return result != null;
            }
        }

        return base.TableExists(tableName);
    }
}
