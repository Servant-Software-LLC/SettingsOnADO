using System.Text;
using Microsoft.Data.Sqlite;
using SettingsOnADO.Tests.TestClasses;
using Xunit;

namespace SettingsOnADO.Tests;

public class SettingsManagerIntegrationTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly SettingsManager settingsManager;

    public SettingsManagerIntegrationTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        settingsManager = new SettingsManager(new SchemaManagerForSqlite(connection));
    }

    [Fact]
    public void ConsumerLifecycle_CanCreateReadAndUpdateSettings()
    {
        var defaults = settingsManager.Get<TestSettings>();
        Assert.Null(defaults.Name);
        Assert.Equal(default(Age), defaults.Age);

        var created = new TestSettings
        {
            Id = 7,
            Name = "Alpha",
            Age = Age.Adult
        };

        settingsManager.Update(created);

        var firstRead = settingsManager.Get<TestSettings>();
        Assert.Equal(created.Id, firstRead.Id);
        Assert.Equal(created.Name, firstRead.Name);
        Assert.Equal(created.Age, firstRead.Age);

        var updated = new TestSettings
        {
            Id = 7,
            Name = "Bravo",
            Age = Age.Old
        };

        settingsManager.Update(updated);

        var secondRead = settingsManager.Get<TestSettings>();
        Assert.Equal(updated.Id, secondRead.Id);
        Assert.Equal(updated.Name, secondRead.Name);
        Assert.Equal(updated.Age, secondRead.Age);
    }

    [Fact]
    public void EncryptionRoundTrip_PersistsCipherTextButReturnsPlainText()
    {
        using var encryptedConnection = new SqliteConnection("Data Source=:memory:");
        encryptedConnection.Open();

        var encryptionProvider = new AesEncryptionProvider(
            Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF"),
            Encoding.UTF8.GetBytes("0123456789ABCDEF"));

        using var encryptedManager = new SettingsManager(
            new SchemaManagerForSqlite(encryptedConnection),
            encryptionProvider);

        var settings = new EncryptedIntegrationSettings
        {
            Id = 1,
            Name = "Prod",
            Password = "P@ssw0rd!"
        };

        encryptedManager.Update(settings);

        using var command = encryptedConnection.CreateCommand();
        command.CommandText = "SELECT Password FROM EncryptedIntegrationSettings LIMIT 1";
        var storedPassword = (string)command.ExecuteScalar()!;

        Assert.NotEqual(settings.Password, storedPassword);

        var roundTripped = encryptedManager.Get<EncryptedIntegrationSettings>();
        Assert.Equal(settings.Password, roundTripped.Password);
        Assert.Equal(settings.Name, roundTripped.Name);
    }

    [Fact]
    public void MultipleSettingsTypes_RemainIsolated()
    {
        var appSettings = new TestSettings
        {
            Id = 2,
            Name = "Application",
            Age = Age.Toddler
        };
        var featureFlags = new FeatureFlagSettings
        {
            FeatureName = "DarkLaunch",
            IsEnabled = true
        };

        settingsManager.Update(appSettings);
        settingsManager.Update(featureFlags);

        var appSettingsRead = settingsManager.Get<TestSettings>();
        var featureFlagsRead = settingsManager.Get<FeatureFlagSettings>();

        Assert.Equal(appSettings.Name, appSettingsRead.Name);
        Assert.Equal(appSettings.Age, appSettingsRead.Age);
        Assert.Equal(featureFlags.FeatureName, featureFlagsRead.FeatureName);
        Assert.Equal(featureFlags.IsEnabled, featureFlagsRead.IsEnabled);
    }

    [Fact]
    public void EdgeCases_NullEmptySpecialCharactersAndLargeValues_RoundTrip()
    {
        var largeValue = new string('x', 16_384);
        var settings = new EdgeCaseSettings
        {
            OptionalValue = null,
            EmptyValue = string.Empty,
            SpecialCharacters = "Line1\nLine2\tSymbols <> & ' \"",
            LargeValue = largeValue
        };

        settingsManager.Update(settings);

        var roundTripped = settingsManager.Get<EdgeCaseSettings>();

        Assert.Null(roundTripped.OptionalValue);
        Assert.Equal(settings.EmptyValue, roundTripped.EmptyValue);
        Assert.Equal(settings.SpecialCharacters, roundTripped.SpecialCharacters);
        Assert.Equal(largeValue, roundTripped.LargeValue);
    }

    public void Dispose()
    {
        settingsManager.Dispose();
        connection.Dispose();
    }

    private sealed class EncryptedIntegrationSettings
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        [Encrypted]
        public string? Password { get; set; }
    }

    private sealed class FeatureFlagSettings
    {
        public string? FeatureName { get; set; }
        public bool IsEnabled { get; set; }
    }

    private sealed class EdgeCaseSettings
    {
        public string? OptionalValue { get; set; }
        public string? EmptyValue { get; set; }
        public string? SpecialCharacters { get; set; }
        public string? LargeValue { get; set; }
    }
}
