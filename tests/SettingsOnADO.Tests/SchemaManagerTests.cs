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
    public void UpdateTableData_UpdatesExistingRow()
    {
        // Arrange
        string tableName = "TestTable";
        _schemaManager.CreateTable(tableName, new[]
        {
            typeof(TestEntity).GetProperty(nameof(TestEntity.Id))!,
            typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!
        });

        _schemaManager.InsertTableData(tableName, new[]
        {
            new InsertValue("Id", 1),
            new InsertValue("Name", "Original")
        });

        // Act
        _schemaManager.UpdateTableData(tableName, new[]
        {
            new InsertValue("Id", 1),
            new InsertValue("Name", "Updated")
        });

        // Assert
        using (var command = _connection.CreateCommand())
        {
            command.CommandText = $"SELECT Id, Name FROM \"{tableName}\"";
            using (var reader = command.ExecuteReader())
            {
                Assert.True(reader.Read());
                Assert.Equal(1, reader.GetInt32(0));
                Assert.Equal("Updated", reader.GetString(1));
                Assert.False(reader.Read()); // Only one row should exist
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

    [Fact]
    public void CreateTable_WithExtendedTypes_CreatesAllColumns()
    {
        // Arrange
        string tableName = "ExtendedTypesTable";
        var properties = typeof(ExtendedTypesEntity).GetProperties();

        // Act
        _schemaManager.CreateTable(tableName, properties);

        // Assert — verify table was created and all columns exist
        using (var command = _connection.CreateCommand())
        {
            command.CommandText = $"PRAGMA table_info(\"{tableName}\")";
            using (var reader = command.ExecuteReader())
            {
                var columns = new List<string>();
                while (reader.Read())
                {
                    columns.Add(reader.GetString(1));
                }

                Assert.Contains("LongValue", columns);
                Assert.Contains("ShortValue", columns);
                Assert.Contains("FloatValue", columns);
                Assert.Contains("DateTimeValue", columns);
                Assert.Contains("DateTimeOffsetValue", columns);
                Assert.Contains("GuidValue", columns);
                Assert.Contains("BinaryData", columns);
            }
        }
    }

    [Fact]
    public void CreateTable_WithNullableInt_CreatesColumn()
    {
        // Arrange
        string tableName = "NullableTable";
        var properties = new[]
        {
            typeof(NullableEntity).GetProperty(nameof(NullableEntity.NullableInt))!
        };

        // Act
        _schemaManager.CreateTable(tableName, properties);

        // Assert
        Assert.True(ColumnExists(tableName, "NullableInt"));
    }

    public void Dispose()
    {
        _schemaManager?.Dispose();
        _connection?.Dispose();
    }

}

internal class ExtendedTypesEntity
{
    public long LongValue { get; set; }
    public short ShortValue { get; set; }
    public float FloatValue { get; set; }
    public DateTime DateTimeValue { get; set; }
    public DateTimeOffset DateTimeOffsetValue { get; set; }
    public Guid GuidValue { get; set; }
    public byte[] BinaryData { get; set; } = Array.Empty<byte>();
}

internal class NullableEntity
{
    public int? NullableInt { get; set; }
}
