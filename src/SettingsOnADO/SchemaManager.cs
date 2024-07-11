using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;

namespace SettingsOnADO;

public class SchemaManager : ISchemaManager
{
    protected readonly DbConnection connection;
    private readonly bool shouldCloseConnection;
    public SchemaManager(DbConnection connection, bool shouldCloseConnection = true)
    {
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
        this.shouldCloseConnection = shouldCloseConnection;

        // Check if the connection is not open and open it if necessary
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }
    }

    public string DataSource => connection.DataSource;

    public virtual DataRow? GetRow(string tableName)
    {
        // Determine if the table exists
        if (!TableExists(tableName))
        {
            return null;
        }

        using (DbCommand command = connection.CreateCommand())
        {
            command.CommandText = $"SELECT * FROM {tableName}";

            DataTable dataTable = new DataTable();
            using (var reader = command.ExecuteReader())
            {
                dataTable.Load(reader);
            }

            return dataTable.Rows.Count > 0 ? dataTable.Rows[0] : null;
        }
    }

    public void InsertTableData(string tableName, IEnumerable<InsertValue> insertValues)
    {
        StringBuilder insertColumns = new($"INSERT INTO {tableName} (");
        StringBuilder insertParameters = new("VALUES (");

        using (DbCommand command = connection.CreateCommand())
        {
            bool firstProperty = true;
            foreach (var insertValue in insertValues)
            {
                if (!firstProperty)
                {
                    insertColumns.Append(", ");
                    insertParameters.Append(", ");
                }
                else
                {
                    firstProperty = false;
                }

                insertColumns.Append(insertValue.ColumnName);
                var paramName = $"@{insertValue.ColumnName}";
                insertParameters.Append(paramName);

                DbParameter param = connection.CreateCommand().CreateParameter();
                param.ParameterName = paramName;
                param.Value = insertValue.Value;

                command.Parameters.Add(param);
            }

            // Finish constructing the SQL INSERT command
            insertColumns.Append(")");
            insertParameters.Append(")");
            var sqlInsert = $"{insertColumns} {insertParameters}";

            command.CommandText = sqlInsert;

            // Execute the command
            command.ExecuteNonQuery();
        }
    }

    public void DeleteTableData(string tableName)
    {
        using (DbCommand command = connection.CreateCommand())
        {
            command.CommandText = $"DELETE FROM {tableName}";

            _ = command.ExecuteNonQuery();
        }
    }

    public void CreateTable(string tableName, IEnumerable<PropertyInfo> properties)
    {
        // Build the CREATE TABLE SQL command
        StringBuilder createTableCommand = new StringBuilder($"CREATE TABLE {tableName} (");
        bool firstProperty = true;

        foreach (PropertyInfo prop in properties)
        {
            if (!firstProperty)
                createTableCommand.Append(", ");
            else
                firstProperty = false;

            // Convert PropertyInfo to SQL column definition
            string columnName = prop.Name;
            string columnType = GetSqlColumnType(prop.PropertyType);

            createTableCommand.Append($"{columnName} {columnType}");
        }

        createTableCommand.Append(")");

        // Execute the command
        using (DbCommand command = connection.CreateCommand())
        {
            command.CommandText = createTableCommand.ToString();
            command.ExecuteNonQuery();
        }
    }

    public void AddColumn(string tableName, PropertyInfo property)
    {
        // Get the SQL column type from the PropertyInfo
        string columnName = property.Name;
        string columnType = GetSqlColumnType(property.PropertyType);

        // Prepare the ALTER TABLE command to add the new column
        string sqlAlterTable = $"ALTER TABLE {tableName} ADD {columnName} {columnType};";

        // Execute the command
        using (DbCommand command = connection.CreateCommand())
        {
            command.CommandText = sqlAlterTable;
            command.ExecuteNonQuery();
        }
    }

    public void DropColumn(string tableName, string columnName)
    {
        // Prepare the ALTER TABLE command to drop the column
        string sql = $"ALTER TABLE {tableName} DROP COLUMN {columnName};";

        // Execute the command
        using (DbCommand command = connection.CreateCommand())
        {
            command.CommandText = sql;

            command.ExecuteNonQuery();
        }
    }

    public void Dispose()
    {
        if (shouldCloseConnection)
        {
            connection.Close();
            connection.Dispose();
        }
    }

    protected virtual string GetSqlColumnType(Type type)
    {
        // Enum types are stored as strings.
        if (type.IsEnum)
            type = typeof(string);

        if (type == typeof(int))
            return "INT";
        else if (type == typeof(decimal) || type == typeof(double))
            return "DECIMAL";
        else if (type == typeof(string))
            return "VARCHAR";
        else if (type == typeof(bool))
            return "BOOLEAN";
        else
            throw new ArgumentException($"Unsupported type: {type.Name}");
    }

    protected virtual bool TableExists(string tableName)
    {
        var tablesList = connection.GetSchema("Tables");
        return tablesList.Rows.Cast<DataRow>().Any(row => row["TABLE_NAME"].ToString() == tableName);
    }

}
