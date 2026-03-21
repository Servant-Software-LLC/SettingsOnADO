using System.Data.Common;
using System.Reflection;
using Microsoft.Data.Sqlite;
using SettingsOnADO.Tests.TestClasses;
using Xunit;

namespace SettingsOnADO.Tests;

public class SchemaManagerTests : IDisposable
{
    private DbConnection _connection;
    private SchemaManager _schemaManager;

    public SchemaManagerTests()
    {
        // Set up the in-memory SQLite connection
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // Initialize SchemaManager with the connection
        _schemaManager = new SchemaManager(_connection, shouldCloseConnection: false);
    }

    [Fact]
    public void CreateTable_CreatesTable_VerifyBySelect()
    {
        // Arrange
        string tableName = "TestTable";
        PropertyInfo[] properties = new PropertyInfo[]
        {
            typeof(TestEntity).GetProperty(nameof(TestEntity.Id))!,
            typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!
        };

        // Act
        _schemaManager.CreateTable(tableName, properties);

        // Assert
        using (var command = _connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@tableName;";
            var param = command.CreateParameter();
            param.ParameterName = "@tableName";
            param.Value = tableName;
            command.Parameters.Add(param);
            var result = command.ExecuteScalar();
            Assert.Equal(tableName, result);
        }
    }

    [Fact]
    public void InsertTableData_InsertsData_VerifyByQuery()
    {
        // Arrange
        string tableName = "TestTable";
        _schemaManager.CreateTable(tableName, new[]
        {
            typeof(TestEntity).GetProperty(nameof(TestEntity.Id))!,
            typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!
        });

        // Act
        _schemaManager.InsertTableData(tableName, new[]
        {
            new InsertValue("Id", 1),
            new InsertValue("Name", "John Doe")
        });

        // Assert
        using (var command = _connection.CreateCommand())
        {
            command.CommandText = $"SELECT Id, Name FROM \"{tableName}\" WHERE Id = 1";
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Assert.Equal(1, reader.GetInt32(0));
                    Assert.Equal("John Doe", reader.GetString(1));
                }
            }
        }
    }

    [Fact]
    public void DropColumn_DropsColumn_VerifyByException()
    {
        // Arrange
        string tableName = "TestTable";
        _schemaManager.CreateTable(tableName, new[]
        {
            typeof(TestEntity).GetProperty(nameof(TestEntity.Id))!,
            typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!
        });
        _schemaManager.AddColumn(tableName, typeof(TestEntity).GetProperty(nameof(TestEntity.Age))!);

        Assert.True(ColumnExists(tableName, "Age"));

        // Act
        _schemaManager.DropColumn(tableName, "Age");

        // Assert
        Assert.False(ColumnExists(tableName, "Age"));
    }

    private bool ColumnExists(string tableName, string columnName)
    {
        using (var command = _connection.CreateCommand())
        {
            command.CommandText = $"PRAGMA table_info(\"{tableName}\")";
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader.GetString(1) == columnName)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    [Fact]
    public void CreateTable_WithQuotesInName_DoesNotAllowInjection()
    {
        // A table name containing a double-quote should be escaped, not allow injection
        string maliciousName = "Test\"Table";
        PropertyInfo[] properties = new PropertyInfo[]
        {
            typeof(TestEntity).GetProperty(nameof(TestEntity.Id))!,
            typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!
        };

        // Act — should create a table with the escaped name, not execute injected SQL
        _schemaManager.CreateTable(maliciousName, properties);

        // Assert — table with the literal name (including the quote) should exist
        using (var command = _connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@tableName;";
            var param = command.CreateParameter();
            param.ParameterName = "@tableName";
            param.Value = maliciousName;
            command.Parameters.Add(param);
            var result = command.ExecuteScalar();
            Assert.Equal(maliciousName, result);
        }
    }

    [Fact]
    public void GetRow_WithQuotesInTableName_ReturnsData()
    {
        // Arrange — use SchemaManagerForSqlite which has a working TableExists for SQLite
        var sqliteSchemaManager = new SchemaManagerForSqlite(_connection, shouldCloseConnection: false);
        string tableName = "My\"Settings";
        PropertyInfo[] properties = new PropertyInfo[]
        {
            typeof(TestEntity).GetProperty(nameof(TestEntity.Id))!,
            typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!
        };
        sqliteSchemaManager.CreateTable(tableName, properties);
        sqliteSchemaManager.InsertTableData(tableName, new[]
        {
            new InsertValue("Id", 1),
            new InsertValue("Name", "Test")
        });

        // Act
        var row = sqliteSchemaManager.GetRow(tableName);

        // Assert
        Assert.NotNull(row);
        Assert.Equal(1, Convert.ToInt32(row["Id"]));
        Assert.Equal("Test", row["Name"].ToString());
    }

    public void Dispose()
    {
        _schemaManager?.Dispose();
        _connection?.Dispose();
    }

}
