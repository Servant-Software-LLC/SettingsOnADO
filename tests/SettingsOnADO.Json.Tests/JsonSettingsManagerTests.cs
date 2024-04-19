using Data.Common.Extension;
using SettingsOnADO.Json.Tests.Models;
using SettingsOnADO.Json.Tests.Utils;
using System.Reflection;
using Xunit;

namespace SettingsOnADO.Json;

public class JsonSettingsManagerTests
{
    [Fact]
    public void Get_WhenCalledInEmptyDatabase_ReturnsInstanceWithDefaults()
    {
        // Provide a unique sandbox for this test
        var sandboxId = $"{GetType().FullName}.{MethodBase.GetCurrentMethod()!.Name}";
        var sandboxConnectionString = ConnectionStrings.Instance.EmptyWithTablesFolderAsDB.Sandbox("Sandbox", sandboxId);

        // Arrange
        var jsonSettingsManager = new JsonSettingsManager(sandboxConnectionString);

        // Act
        var result = jsonSettingsManager.Get<GeneralSettings>();

        // Assert
        Assert.NotNull(result);

        //Check that the default values are set
        var expected = new GeneralSettings();
        Assert.Equal(expected.DefaultDatabaseName, result.DefaultDatabaseName);
        Assert.Equal(expected.SeedDatabases, result.SeedDatabases);
        Assert.Equal(expected.CaseSensitive, result.CaseSensitive);
        Assert.Equal(expected.MaxAllTableRowCount, result.MaxAllTableRowCount);
    }

    /// <summary>
    /// Test that there is no exception when a setting is deprecated
    /// </summary>
    [Fact]
    public void Get_WhenSettingIsDeprecated_DoesNotThrowException()
    {
        // Provide a unique sandbox for this test
        var sandboxId = $"{GetType().FullName}.{MethodBase.GetCurrentMethod()!.Name}";
        var sandboxConnectionString = ConnectionStrings.Instance.TablesFolderAsDB.Sandbox("Sandbox", sandboxId);

        // Arrange
        var jsonSettingsManager = new JsonSettingsManager(sandboxConnectionString);

        // Act
        var result = jsonSettingsManager.Get<Tests.Models.NextVersion.GeneralSettings>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(true, result.CaseSensitive);
        Assert.Equal("NewMockDB", result.DefaultDatabaseName);
        Assert.Equal(3000, result.MaxAllTableRowCount);
        Assert.Equal(true, result.SeedDatabases);        
    }

    /// <summary>
    /// When a setting property is added, the other setting properties should still be returned
    /// </summary>
    [Fact]
    public void Get_WhenSettingIsAddedInThisVersion_ReturnsInstanceWithValuesFromDatabase()
    {
        // Provide a unique sandbox for this test
        var sandboxId = $"{GetType().FullName}.{MethodBase.GetCurrentMethod()!.Name}";
        var sandboxConnectionString = ConnectionStrings.Instance.TablesFolderAsDB.Sandbox("Sandbox", sandboxId);

        // Arrange
        var jsonSettingsManager = new JsonSettingsManager(sandboxConnectionString);

        // Act
        var result = jsonSettingsManager.Get<Tests.Models.NextVersion.GeneralSettings>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(true, result.CaseSensitive);
        Assert.Equal("NewMockDB", result.DefaultDatabaseName);
        Assert.Equal(3000, result.MaxAllTableRowCount);
        Assert.Equal(true, result.SeedDatabases);
        Assert.Equal(7, result.SettingsAddedInThisVersion); // 7 is the default value for this property
    }

    [Fact]
    public void Update_WhenCalledWithNewValues_ReturnsInstanceWithNewValues()
    {
        // Provide a unique sandbox for this test
        var sandboxId = $"{GetType().FullName}.{MethodBase.GetCurrentMethod()!.Name}";
        var sandboxConnectionString = ConnectionStrings.Instance.EmptyWithTablesFolderAsDB.Sandbox("Sandbox", sandboxId);

        // Arrange
        var jsonSettingsManager = new JsonSettingsManager(sandboxConnectionString);
        var newSettings = new GeneralSettings
        {
            DefaultDatabaseName = "NewMockDB",
            SeedDatabases = true,
            CaseSensitive = true,
            MaxAllTableRowCount = 2000
        };

        // Act
        jsonSettingsManager.Update(newSettings);
        var result = jsonSettingsManager.Get<GeneralSettings>();

        // Assert
        Assert.NotNull(result);

        //Check that the new values are set
        Assert.Equal(newSettings.DefaultDatabaseName, result.DefaultDatabaseName);
        Assert.Equal(newSettings.SeedDatabases, result.SeedDatabases);
        Assert.Equal(newSettings.CaseSensitive, result.CaseSensitive);
        Assert.Equal(newSettings.MaxAllTableRowCount, result.MaxAllTableRowCount);
    }

    [Fact]
    public void Update_WhenSettingIsDeprecated_SettingRemovedFromStorage()
    {
        // Provide a unique sandbox for this test
        var sandboxId = $"{GetType().FullName}.{MethodBase.GetCurrentMethod()!.Name}";
        var sandboxConnectionString = ConnectionStrings.Instance.TablesFolderAsDB.Sandbox("Sandbox", sandboxId);

        // Arrange
        var jsonSettingsManager = new JsonSettingsManager(sandboxConnectionString);
        var newSettings = new Tests.Models.NextVersion.GeneralSettings
        {
            DefaultDatabaseName = "NewMockDB",
            SeedDatabases = true,
            CaseSensitive = true,
            MaxAllTableRowCount = 2000,
            SettingsAddedInThisVersion = 7
        };

        // Read all the text from the GeneralSettings.json file
        var jsonText = File.ReadAllText(Path.Combine(sandboxConnectionString.DataSource, "GeneralSettings.json"));
        Assert.Contains("SettingToBeDeprecated", jsonText);


        // Act
        jsonSettingsManager.Update(newSettings);
        var result = jsonSettingsManager.Get<Tests.Models.NextVersion.GeneralSettings>();

        // Assert

        // Read all the text from the GeneralSettings.json file
        jsonText = File.ReadAllText(Path.Combine(sandboxConnectionString.DataSource, "GeneralSettings.json"));

        // Assert that the setting that was deprecated is not in the json text
        Assert.DoesNotContain("SettingToBeDeprecated", jsonText);
    }

    [Fact]
    public void Update_EmptyDatabase_UpdateWithDefaultsFollowedByPropertyChange()
    {
        // Provide a unique sandbox for this test
        var sandboxId = $"{GetType().FullName}.{MethodBase.GetCurrentMethod()!.Name}";
        var sandboxConnectionString = ConnectionStrings.Instance.EmptyWithTablesFolderAsDB.Sandbox("Sandbox", sandboxId);

        // Arrange
        var jsonSettingsManager = new JsonSettingsManager(sandboxConnectionString);
        var settings = new GeneralSettings();

        jsonSettingsManager.Update(settings);

        // Read all the text from the GeneralSettings.json file
        var jsonText = File.ReadAllText(Path.Combine(sandboxConnectionString.DataSource, "GeneralSettings.json"));
        Assert.DoesNotContain("null", jsonText);

        // Act
        settings.MaxAllTableRowCount = 1001;
        jsonSettingsManager.Update(settings);

        // Assert

        // Read all the text from the GeneralSettings.json file
        jsonText = File.ReadAllText(Path.Combine(sandboxConnectionString.DataSource, "GeneralSettings.json"));

        // Assert that the setting that was deprecated is not in the json text
        Assert.DoesNotContain("null", jsonText);
    }
}
