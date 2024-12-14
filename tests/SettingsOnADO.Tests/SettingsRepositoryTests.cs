using System.Data;
using System.Reflection;
using Moq;
using SettingsOnADO.Tests.TestClasses;
using Xunit;

namespace SettingsOnADO.Tests;

public class SettingsRepositoryTests
{
    [Fact]
    public void Get_SettingsObject_ReturnsPopulatedSettings()
    {
        // Arrange
        var schemaManagerMock = new Mock<ISchemaManager>();
        var dataTable = new DataTable();
        dataTable.Columns.Add("Id", typeof(int));
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("Age", typeof(string));
        var row = dataTable.NewRow();
        row["Id"] = 1;
        row["Name"] = "TestName";
        row["Age"] = "Adult";
        dataTable.Rows.Add(row);

        schemaManagerMock.Setup(m => m.GetRow(It.IsAny<string>())).Returns(row);

        var repository = new SettingsRepository(schemaManagerMock.Object, null);

        // Act
        var result = repository.Get<TestSettings>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestName", result.Name);
        Assert.Equal(1, result.Id);
        Assert.Equal(Age.Adult, result.Age);
    }

    [Fact]
    public void Get_DecryptsPropertiesWithEncryptedAttribute()
    {
        // Arrange
        var encryptedPassword = "EncryptedString";
        var decryptedPassword = "Test";
        var dataTable = new DataTable();
        dataTable.Columns.Add("Id", typeof(int));
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("Password", typeof(string));
        var row = dataTable.NewRow();
        row["Id"] = 1;
        row["Name"] = "TestName";
        row["Password"] = encryptedPassword;
        dataTable.Rows.Add(row);

        var mockSchemaManager = new Mock<ISchemaManager>();
        mockSchemaManager.Setup(m => m.GetRow(It.IsAny<string>())).Returns(row);

        var mockEncryptionProvider = new Mock<IEncryptionProvider>();
        mockEncryptionProvider.Setup(e => e.Decrypt(It.IsAny<string>())).Returns(decryptedPassword);

        var settingsRepository = new SettingsRepository(mockSchemaManager.Object, mockEncryptionProvider.Object);

        // Act
        var result = settingsRepository.Get<TestSettingsWithEncrypted>();

        // Assert
        mockEncryptionProvider.Verify(e => e.Decrypt(encryptedPassword), Times.Once);
        Assert.Equal(decryptedPassword, result.Password);
    }

    [Fact]
    public void Update_NewEntity_CreatesTableAndInsertsData()
    {
        // Arrange
        var schemaManagerMock = new Mock<ISchemaManager>();
        schemaManagerMock.Setup(m => m.GetRow(It.IsAny<string>())).Returns(null as DataRow); // Simulate table does not exist

        var repository = new SettingsRepository(schemaManagerMock.Object, null);
        var testSettings = new TestSettings { Id = 2, Name = "NewName", Age=Age.Baby };

        // Act
        repository.Update(testSettings);

        // Verify that CreateTable and InsertTableData were called
        schemaManagerMock.Verify(m => m.CreateTable("TestSettings", It.IsAny<IEnumerable<PropertyInfo>>()), Times.Once);
        schemaManagerMock.Verify(m => m.InsertTableData("TestSettings", It.IsAny<IEnumerable<InsertValue>>()), Times.Once);
    }

    [Fact]
    public void Update_ExistingEntity_UpdatesData()
    {
        // Arrange
        var schemaManagerMock = new Mock<ISchemaManager>();
        var dataTable = new DataTable();
        dataTable.Columns.Add("Id", typeof(int));
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("Age", typeof(string));
        var row = dataTable.NewRow();
        row["Id"] = 1;
        row["Name"] = "OriginalName";
        row["Age"] = "Toddler";
        dataTable.Rows.Add(row);

        schemaManagerMock.Setup(m => m.GetRow(It.IsAny<string>())).Returns(row);

        var repository = new SettingsRepository(schemaManagerMock.Object, null);
        var updatedSettings = new TestSettings { Id = 1, Name = "UpdatedName", Age = Age.Adult };

        // Act
        repository.Update(updatedSettings);

        // Verify that DeleteTableData and InsertTableData were called
        schemaManagerMock.Verify(m => m.DeleteTableData("TestSettings"), Times.Once);
        schemaManagerMock.Verify(m => m.InsertTableData("TestSettings", It.IsAny<IEnumerable<InsertValue>>()), Times.Once);
    }

    [Fact]
    public void Update_EncryptsPropertiesWithEncryptedAttribute()
    {
        // Arrange
        var password = "Test"; 
        var settings = new TestSettingsWithEncrypted 
        {
            Id = 1,
            Name = "TestName",
            Password = password 
        };

        var mockSchemaManager = new Mock<ISchemaManager>();
        var mockEncryptionProvider = new Mock<IEncryptionProvider>();        
        mockEncryptionProvider.Setup(e => e.Encrypt(It.IsAny<string>())).Returns("EncryptedString");

        var settingsRepository = new SettingsRepository(mockSchemaManager.Object, mockEncryptionProvider.Object);

        // Act
        settingsRepository.Update(settings);

        // Assert
        mockEncryptionProvider.Verify(e => e.Encrypt(password), Times.Once);
    }

}

