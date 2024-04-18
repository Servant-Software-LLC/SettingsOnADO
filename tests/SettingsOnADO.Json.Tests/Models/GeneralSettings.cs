namespace SettingsOnADO.Json.Tests.Models;

public class GeneralSettings
{
    [JsonDescription("This is the database name used in the MockDB GraphQL API when it is not provided for the Name field of the Database object.")]
    public string DefaultDatabaseName { get; set; } = "MockDB";

    public bool SeedDatabases { get; set; } = false;

    public bool CaseSensitive { get; set; } = false;

    public int MaxAllTableRowCount { get; set; } = 1000;

    public int SettingToBeDeprecated { get; set; }
}
