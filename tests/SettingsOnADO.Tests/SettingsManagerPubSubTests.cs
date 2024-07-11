using System.Data.Common;
using Microsoft.Data.Sqlite;
using Xunit;
using SettingsOnADO.Tests.TestClasses;

namespace SettingsOnADO.Tests;

public class SettingsManagerPubSubTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly SettingsManager _settingsManager;

    public SettingsManagerPubSubTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        SchemaManagerForSqlite schemaManagerForSqlite = new(_connection);
        _settingsManager = new SettingsManager(schemaManagerForSqlite);
    }

    [Fact]
    public void Subscribe_WhenSettingsUpdated_HandlerIsCalled()
    {
        // Arrange
        var handlerCalled = false;
        var oldSettings = new TestSettings { Id = 1, Name = "Old Name", Age = Age.Adult };
        var newSettings = new TestSettings { Id = 1, Name = "New Name", Age = Age.Adult };

        _settingsManager.Update(oldSettings);

        _settingsManager.Subscribe<TestSettings>(args =>
        {
            handlerCalled = true;
            Assert.Equal(oldSettings.Name, args.OldSettings.Name);
            Assert.Equal(newSettings.Name, args.NewSettings.Name);
        });

        // Act
        _settingsManager.Update(newSettings);

        // Assert
        Assert.True(handlerCalled);
    }

    [Fact]
    public void Subscribe_MultipleSubscribers_AllHandlersAreCalled()
    {
        // Arrange
        var handler1Called = false;
        var handler2Called = false;
        var settings = new TestSettings { Id = 1, Name = "Test", Age = Age.Adult };

        _settingsManager.Subscribe<TestSettings>(_ => handler1Called = true);
        _settingsManager.Subscribe<TestSettings>(_ => handler2Called = true);

        // Act
        _settingsManager.Update(settings);

        // Assert
        Assert.True(handler1Called);
        Assert.True(handler2Called);
    }

    [Fact]
    public void Unsubscribe_WhenHandlerRemoved_HandlerIsNotCalled()
    {
        // Arrange
        var handlerCalled = false;
        var settings = new TestSettings { Id = 1, Name = "Test", Age = Age.Adult };

        Action<SettingsChangeEventArgs<TestSettings>> handler = _ => handlerCalled = true;
        _settingsManager.Subscribe(handler);
        _settingsManager.Unsubscribe(handler);

        // Act
        _settingsManager.Update(settings);

        // Assert
        Assert.False(handlerCalled);
    }

    [Fact]
    public void Subscribe_DifferentSettingsTypes_OnlyRelevantHandlersAreCalled()
    {
        // Arrange
        var testSettingsHandlerCalled = false;
        var testSettingsWithEncryptedHandlerCalled = false;
        var testSettings = new TestSettings { Id = 1, Name = "Test", Age = Age.Adult };
        var testSettingsWithEncrypted = new TestSettingsWithEncrypted { Id = 1, Name = "Test", Password = "password" };

        _settingsManager.Subscribe<TestSettings>(_ => testSettingsHandlerCalled = true);
        _settingsManager.Subscribe<TestSettingsWithEncrypted>(_ => testSettingsWithEncryptedHandlerCalled = true);

        // Act
        _settingsManager.Update(testSettings);

        // Assert
        Assert.True(testSettingsHandlerCalled);
        Assert.False(testSettingsWithEncryptedHandlerCalled);
    }

    public void Dispose()
    {
        _settingsManager.Dispose();
        _connection.Dispose();
    }
}