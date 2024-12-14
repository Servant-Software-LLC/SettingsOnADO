using Moq;
using SettingsOnADO.Tests.TestClasses;
using Xunit;

namespace SettingsOnADO.Tests;

public class SettingsManagerTests : IDisposable
{
    private readonly Mock<ISettingsRepository> _mockSettingsRepository;
    private readonly SettingsManager _settingsManager;

    public SettingsManagerTests()
    {
        _mockSettingsRepository = new Mock<ISettingsRepository>();
        _settingsManager = new SettingsManager(_mockSettingsRepository.Object);
    }

    [Fact]
    public void DataSource_ShouldReturnNoneIfNoSchemaManager()
    {
        // Act
        var dataSource = _settingsManager.DataSource;

        // Assert
        Assert.Equal("<None>", dataSource);
    }

    [Fact]
    public void Get_ShouldReturnSettingsEntityFromRepository()
    {
        // Arrange
        var testSettings = new TestSettings { Id = 1, Name = "Test Name" };
        _mockSettingsRepository.Setup(r => r.Get<TestSettings>()).Returns(testSettings);

        // Act
        var result = _settingsManager.Get<TestSettings>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testSettings.Id, result.Id);
        Assert.Equal(testSettings.Name, result.Name);
        _mockSettingsRepository.Verify(r => r.Get<TestSettings>(), Times.Once);
    }

    [Fact]
    public void Update_ShouldCallRepositoryUpdateAndNotifySubscribers()
    {
        // Arrange
        var oldSettings = new TestSettings { Id = 1, Name = "Old Name" };
        var newSettings = new TestSettings { Id = 1, Name = "New Name" };
        _mockSettingsRepository.Setup(r => r.Get<TestSettings>()).Returns(oldSettings);

        var subscriberCalled = false;
        _settingsManager.Subscribe<TestSettings>(args =>
        {
            Assert.Equal(oldSettings, args.OldSettings);
            Assert.Equal(newSettings, args.NewSettings);
            subscriberCalled = true;
        });

        // Act
        _settingsManager.Update(newSettings);

        // Assert
        Assert.True(subscriberCalled);
        _mockSettingsRepository.Verify(r => r.Update(newSettings), Times.Once);
    }

    [Fact]
    public void Subscribe_ShouldCallSubscriberWhenUpdateIsCalled()
    {
        // Arrange
        var oldSettings = new TestSettings { Id = 1, Name = "Old Name" };
        var newSettings = new TestSettings { Id = 1, Name = "New Name" };
        _mockSettingsRepository.Setup(r => r.Get<TestSettings>()).Returns(oldSettings);

        var subscriberCalled = false;
        _settingsManager.Subscribe<TestSettings>(args =>
        {
            Assert.Equal(oldSettings, args.OldSettings);
            Assert.Equal(newSettings, args.NewSettings);
            subscriberCalled = true;
        });

        // Act
        _settingsManager.Update(newSettings);

        // Assert
        Assert.True(subscriberCalled);
    }

    [Fact]
    public void Unsubscribe_ShouldPreventSubscriberFromBeingNotified()
    {
        // Arrange
        var oldSettings = new TestSettings { Id = 1, Name = "Old Name" };
        var newSettings = new TestSettings { Id = 1, Name = "New Name" };
        _mockSettingsRepository.Setup(r => r.Get<TestSettings>()).Returns(oldSettings);

        var subscriberCalled = false;
        Action<SettingsChangeEventArgs<TestSettings>> handler = args => subscriberCalled = true;
        _settingsManager.Subscribe(handler);
        _settingsManager.Unsubscribe(handler);

        // Act
        _settingsManager.Update(newSettings);

        // Assert
        Assert.False(subscriberCalled);
    }

    [Fact]
    public void Dispose_ShouldNotThrow_WhenNoSchemaManagerIsProvided()
    {
        // Act & Assert
        _settingsManager.Dispose(); // No schemaManager to dispose, should not throw
    }

    public void Dispose()
    {
        _settingsManager?.Dispose();
    }
}
