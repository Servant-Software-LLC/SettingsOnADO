using Moq;
using SettingsOnADO.Tests.TestClasses;
using Xunit;

namespace SettingsOnADO.Tests;

public class SettingsManagerWithCacheTests
{
    private readonly Mock<ISettingsManager> mockSettingsManager;
    private readonly SettingsManagerWithCache settingsManagerWithCache;

    public SettingsManagerWithCacheTests()
    {
        mockSettingsManager = new Mock<ISettingsManager>();
        settingsManagerWithCache = new SettingsManagerWithCache(mockSettingsManager.Object);
    }

    [Fact]
    public void SetCacheValue_ShouldCacheSettings()
    {
        // Arrange
        var testSettings = new TestSettings { Id = 1, Name = "Test Name" };

        // Act
        settingsManagerWithCache.SetCacheValue(testSettings);

        // Assert
        // Retrieve the cached value via Get to verify it's cached
        var cachedSettings = settingsManagerWithCache.Get<TestSettings>();
        Assert.Equal(testSettings.Id, cachedSettings.Id);
        Assert.Equal(testSettings.Name, cachedSettings.Name);
    }

    [Fact]
    public void SetCacheValue_Twice_ShouldOverrideCachedSettings()
    {
        // Arrange
        var firstSettings = new TestSettings { Id = 1, Name = "First Name" };
        var secondSettings = new TestSettings { Id = 2, Name = "Second Name" };

        // Act
        settingsManagerWithCache.SetCacheValue(firstSettings);   // Set first value
        settingsManagerWithCache.SetCacheValue(secondSettings);  // Override with second value

        // Assert
        // Verify that the second settings value is returned from the cache
        var cachedSettings = settingsManagerWithCache.Get<TestSettings>();
        Assert.Equal(secondSettings.Id, cachedSettings.Id);
        Assert.Equal(secondSettings.Name, cachedSettings.Name);
    }

    [Fact]
    public void SetCacheValue_ShouldNotTriggerNotificationOnInnerManager()
    {
        // Arrange
        var cachedSettings = new TestSettings { Id = 1, Name = "Cached Name" };

        // Mock the Subscribe behavior of the underlying settings manager
        var subscriberCalled = false;
        mockSettingsManager.Setup(m => m.Subscribe<TestSettings>(It.IsAny<Action<SettingsChangeEventArgs<TestSettings>>>())).Callback(
            (Action<SettingsChangeEventArgs<TestSettings>> handler) =>
            {
                subscriberCalled = true; // This should NOT happen when setting the cache value
            });

        // Act
        settingsManagerWithCache.SetCacheValue(cachedSettings); // Set cache value

        // Assert
        // Since we are setting a cached value, the notification should NOT be triggered
        Assert.False(subscriberCalled);
    }

    [Fact]
    public void RemoveCacheValue_ShouldRemoveCachedSettings()
    {
        // Arrange
        var testSettings = new TestSettings { Id = 1, Name = "Test Name" };
        settingsManagerWithCache.SetCacheValue(testSettings);

        // Reset the verification call counts, since Get() is called by SetCacheValue() in the Arrange.
        mockSettingsManager.Invocations.Clear(); // This clears the invocation history without losing setup

        // Act
        var removed = settingsManagerWithCache.RemoveCacheValue<TestSettings>();

        // Assert
        Assert.True(removed);

        // Mock the underlying settings manager to return a different value
        var fallbackSettings = new TestSettings { Id = 2, Name = "From Inner Manager" };
        mockSettingsManager.Setup(m => m.Get<TestSettings>()).Returns(fallbackSettings);

        // After removing from the cache, it should retrieve from the inner settings manager
        var result = settingsManagerWithCache.Get<TestSettings>();

        // Verify that the Get method was called on the underlying inner settings manager
        mockSettingsManager.Verify(m => m.Get<TestSettings>(), Times.Once);

        // Also assert that the value returned is from the underlying manager
        Assert.Equal(fallbackSettings.Id, result.Id);
        Assert.Equal(fallbackSettings.Name, result.Name);
    }

    [Fact]
    public void Get_ShouldReturnCachedValueIfPresent()
    {
        // Arrange
        var testSettings = new TestSettings { Id = 1, Name = "Cached Name" };
        settingsManagerWithCache.SetCacheValue(testSettings);

        // Reset the verification call counts, since Get() is called by SetCacheValue() in the Arrange.
        mockSettingsManager.Invocations.Clear(); // This clears the invocation history without losing setup

        // Act
        var result = settingsManagerWithCache.Get<TestSettings>();

        // Assert
        Assert.Equal(testSettings.Id, result.Id);
        Assert.Equal(testSettings.Name, result.Name);
        mockSettingsManager.Verify(m => m.Get<TestSettings>(), Times.Never);
    }

    [Fact]
    public void Get_ShouldFallbackToInnerManagerIfNoCache()
    {
        // Arrange
        var testSettings = new TestSettings { Id = 2, Name = "From Inner Manager" };
        mockSettingsManager.Setup(m => m.Get<TestSettings>()).Returns(testSettings);

        // Act
        var result = settingsManagerWithCache.Get<TestSettings>();

        // Assert
        Assert.Equal(testSettings.Id, result.Id);
        Assert.Equal(testSettings.Name, result.Name);
        mockSettingsManager.Verify(m => m.Get<TestSettings>(), Times.Once);
    }

    [Fact]
    public void Update_ShouldCallInnerManagerUpdate()
    {
        // Arrange
        var testSettings = new TestSettings { Id = 1, Name = "Updated Name" };

        // Act
        settingsManagerWithCache.Update(testSettings);

        // Assert
        mockSettingsManager.Verify(m => m.Update(testSettings), Times.Once);
    }

    [Fact]
    public void Dispose_ShouldDisposeInnerSettingsManager()
    {
        // Act
        settingsManagerWithCache.Dispose();

        // Assert
        mockSettingsManager.Verify(m => m.Dispose(), Times.Once);
    }

    [Fact]
    public void Subscribe_ShouldCallSubscriberWhenUpdateIsCalled()
    {
        // Arrange
        var oldSettings = new TestSettings { Id = 1, Name = "Old Name" };
        var newSettings = new TestSettings { Id = 1, Name = "New Name" };

        mockSettingsManager.Setup(m => m.Get<TestSettings>()).Returns(oldSettings);

        var subscriberCalled = false;
        settingsManagerWithCache.Subscribe<TestSettings>(args =>
        {
            // Check that the old and new values are passed to the subscriber
            Assert.Equal(oldSettings.Id, args.OldSettings.Id);
            Assert.Equal(newSettings.Id, args.OldSettings.Id);
            subscriberCalled = true;
        });

        // Act
        settingsManagerWithCache.Update(newSettings);

        // Assert
        Assert.True(subscriberCalled);
    }

    [Fact]
    public void Subscribe_ShouldNotCallSubscriberWhenCacheIsSet()
    {
        // Arrange
        var cachedSettings = new TestSettings { Id = 1, Name = "Cached Name" };
        settingsManagerWithCache.SetCacheValue(cachedSettings);

        var subscriberCalled = false;
        settingsManagerWithCache.Subscribe<TestSettings>(args =>
        {
            subscriberCalled = true;
        });

        // Act
        var result = settingsManagerWithCache.Get<TestSettings>();

        // Assert
        // Subscriber should not be called since the value comes from the cache
        Assert.False(subscriberCalled);
        Assert.Equal(cachedSettings.Id, result.Id);
        Assert.Equal(cachedSettings.Name, result.Name);
    }

    [Fact]
    public void Unsubscribe_ShouldPreventSubscriberFromBeingNotified()
    {
        // Arrange
        var oldSettings = new TestSettings { Id = 1, Name = "Old Name" };
        var newSettings = new TestSettings { Id = 1, Name = "New Name" };

        mockSettingsManager.Setup(m => m.Get<TestSettings>()).Returns(oldSettings);

        var subscriberCalled = false;
        Action<SettingsChangeEventArgs<TestSettings>> handler = args =>
        {
            subscriberCalled = true;
        };

        settingsManagerWithCache.Subscribe(handler);
        settingsManagerWithCache.Unsubscribe(handler);

        // Act
        settingsManagerWithCache.Update(newSettings);

        // Assert
        // Subscriber should not be called since it was unsubscribed
        Assert.False(subscriberCalled);
    }

}
