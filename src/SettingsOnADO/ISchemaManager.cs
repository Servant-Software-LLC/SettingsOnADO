using System.Data;
using System.Reflection;

namespace SettingsOnADO;

public interface ISchemaManager : IDisposable
{
    string DataSource { get; }

    DataRow? GetRow(string tableName);

    void InsertTableData(string tableName, IEnumerable<InsertValue> insertValues);
    void DeleteTableData(string tableName);
    void CreateTable(string tableName, IEnumerable<PropertyInfo> properties);
    void AddColumn(string tableName, PropertyInfo property);
    void DropColumn(string tableName, string columnName);
}
