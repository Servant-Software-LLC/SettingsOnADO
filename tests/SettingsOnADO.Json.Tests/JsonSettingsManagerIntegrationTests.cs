using Data.Common.Extension;
using Data.Common.Utils.ConnectionString;
using SettingsOnADO.Json.Tests.Models;
using SettingsOnADO.Json.Tests.Utils;
using System.Collections.Concurrent;
using System.Reflection;
using Xunit;

namespace SettingsOnADO.Json;

public class JsonSettingsManagerIntegrationTests
{
    [Fact]
    public void MultipleSettingsTypes_RoundTripAndStayIsolated()
    {
        var sandboxConnectionString = CreateSandboxConnectionString();
        var jsonSettingsManager = new JsonSettingsManager(sandboxConnectionString);

        var generalSettings = new GeneralSettings
        {
            DefaultDatabaseName = "ProdDb",
            SeedDatabases = true,
            CaseSensitive = false,
            MaxAllTableRowCount = 5000
        };
        var providerSettings = new AdoProviderSettings
        {
            CertificatePolicy = CertificatePolicyEnum.TrustSelfSignedCertificates
        };

        jsonSettingsManager.Update(generalSettings);
        jsonSettingsManager.Update(providerSettings);

        var generalRead = jsonSettingsManager.Get<GeneralSettings>();
        var providerRead = jsonSettingsManager.Get<AdoProviderSettings>();

        Assert.Equal(generalSettings.DefaultDatabaseName, generalRead.DefaultDatabaseName);
        Assert.Equal(generalSettings.MaxAllTableRowCount, generalRead.MaxAllTableRowCount);
        Assert.Equal(providerSettings.CertificatePolicy, providerRead.CertificatePolicy);
    }

    [Fact]
    public void ProviderInterop_WritesFilesThatCanBeReadBack()
    {
        var sandboxConnectionString = CreateSandboxConnectionString();
        var jsonSettingsManager = new JsonSettingsManager(sandboxConnectionString);
        var settings = new GeneralSettings
        {
            DefaultDatabaseName = "InteropDb",
            SeedDatabases = true,
            CaseSensitive = true,
            MaxAllTableRowCount = 2500
        };

        jsonSettingsManager.Update(settings);

        var settingsFile = Path.Combine(sandboxConnectionString.DataSource, "GeneralSettings.json");
        var jsonText = File.ReadAllText(settingsFile);
        var roundTripped = jsonSettingsManager.Get<GeneralSettings>();

        Assert.Contains("InteropDb", jsonText);
        Assert.Contains("2500", jsonText);
        Assert.Equal(settings.DefaultDatabaseName, roundTripped.DefaultDatabaseName);
        Assert.Equal(settings.MaxAllTableRowCount, roundTripped.MaxAllTableRowCount);
    }

    [Fact]
    public async Task ConcurrentReads_DuringTypeRegistration_DoNotThrowAndLeaveReadableState()
    {
        var sandboxConnectionString = CreateSandboxConnectionString();
        var jsonSettingsManager = new JsonSettingsManager(sandboxConnectionString);
        var errors = new ConcurrentQueue<Exception>();

        var tasks = Enumerable.Range(0, 8).Select(worker => Task.Run(() =>
        {
            for (var iteration = 0; iteration < 25; iteration++)
            {
                try
                {
                    var general = jsonSettingsManager.Get<GeneralSettings>();
                    var provider = jsonSettingsManager.Get<AdoProviderSettings>();

                    Assert.NotNull(general);
                    Assert.NotNull(provider);
                }
                catch (Exception ex)
                {
                    errors.Enqueue(ex);
                }
            }
        }));

        await Task.WhenAll(tasks);

        Assert.Empty(errors);
        Assert.NotNull(jsonSettingsManager.Get<GeneralSettings>());
        Assert.NotNull(jsonSettingsManager.Get<AdoProviderSettings>());
    }

    private FileConnectionString CreateSandboxConnectionString()
    {
        var sandboxId = $"{GetType().FullName}.{MethodBase.GetCurrentMethod()!.Name}.{Guid.NewGuid():N}";
        return ConnectionStrings.Instance.EmptyWithTablesFolderAsDB.Sandbox("Sandbox", sandboxId);
    }
}
