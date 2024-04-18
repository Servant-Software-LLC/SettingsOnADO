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
        var row = dataTable.NewRow();
        row["Id"] = 1;
        row["Name"] = "TestName";
        dataTable.Rows.Add(row);

        schemaManagerMock.Setup(m => m.GetRow(It.IsAny<string>())).Returns(row);

        var repository = new SettingsRepository(schemaManagerMock.Object);

        // Act
        var result = repository.Get<TestSettings>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestName", result.Name);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public void Update_NewEntity_CreatesTableAndInsertsData()
    {
        // Arrange
        var schemaManagerMock = new Mock<ISchemaManager>();
        schemaManagerMock.Setup(m => m.GetRow(It.IsAny<string>())).Returns((DataRow)null); // Simulate table does not exist

        var repository = new SettingsRepository(schemaManagerMock.Object);
        var testSettings = new TestSettings { Id = 2, Name = "NewName" };

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
        var row = dataTable.NewRow();
        row["Id"] = 1;
        row["Name"] = "OriginalName";
        dataTable.Rows.Add(row);

        schemaManagerMock.Setup(m => m.GetRow(It.IsAny<string>())).Returns(row);

        var repository = new SettingsRepository(schemaManagerMock.Object);
        var updatedSettings = new TestSettings { Id = 1, Name = "UpdatedName" };

        // Act
        repository.Update(updatedSettings);

        // Verify that DeleteTableData and InsertTableData were called
        schemaManagerMock.Verify(m => m.DeleteTableData("TestSettings"), Times.Once);
        schemaManagerMock.Verify(m => m.InsertTableData("TestSettings", It.IsAny<IEnumerable<InsertValue>>()), Times.Once);
    }
}

