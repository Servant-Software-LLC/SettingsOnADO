using Data.Common.Extension;
using Data.Common.Utils.ConnectionString;

namespace SettingsOnADO.Json.Tests.Utils;

public class ConnectionStrings
{
    protected virtual string FolderWithTables => Path.Combine(FileConnectionStringTestsExtensions.SourcesFolder, "Database");
    protected virtual string FolderEmptyWithTables => Path.Combine(FileConnectionStringTestsExtensions.SourcesFolder, "EmptyDatabase");

    public virtual FileConnectionString TablesFolderAsDB => new FileConnectionString() { DataSource = FolderWithTables, Formatted = true };
    public virtual FileConnectionString EmptyWithTablesFolderAsDB => new FileConnectionString() { DataSource = FolderEmptyWithTables, Formatted = true };

    public new static ConnectionStrings Instance => new ConnectionStrings();
}
