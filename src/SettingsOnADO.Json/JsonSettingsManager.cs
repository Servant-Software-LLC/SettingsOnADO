using Data.Common.Utils.ConnectionString;
using Microsoft.Extensions.Logging;

namespace SettingsOnADO.Json;

public class JsonSettingsManager : SettingsManager
{
    private readonly JsonConnectionEx jsonConnectionEx;

    private static string basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
    private const string settingsFolderName = "Settings";

    public JsonSettingsManager(string productName, LogLevel? logLevel = null, IEncryptionProvider? encryptionProvider = null)
        : this(new FileInfo(Path.Combine(basePath, productName, settingsFolderName)), logLevel, encryptionProvider)
    {

    }

    public JsonSettingsManager(string companyName, string productName, LogLevel? logLevel = null, IEncryptionProvider? encryptionProvider = null)
        : this(new FileInfo(Path.Combine(basePath, companyName, productName, settingsFolderName)), logLevel, encryptionProvider)
    {

    }

    public JsonSettingsManager(FileConnectionString connectionString, IEncryptionProvider? encryptionProvider = null)
        : this(new JsonConnectionEx(connectionString), encryptionProvider) { }

    public JsonSettingsManager(FileInfo jsonSettingsPath, LogLevel? logLevel = null, IEncryptionProvider? encryptionProvider = null)
        : this(new JsonConnectionEx(new FileConnectionString()
        {
            DataSource = jsonSettingsPath.FullName,
            CreateIfNotExist = true,
            LogLevel = logLevel,
            Formatted = true
        }), encryptionProvider) { }

    private JsonSettingsManager(JsonConnectionEx jsonConnectionEx, IEncryptionProvider? encryptionProvider = null)
        : base(jsonConnectionEx, true, encryptionProvider)
    {
        this.jsonConnectionEx = jsonConnectionEx;
    }

    public override TSettingsEntity Get<TSettingsEntity>()
    {
        //Check if this TSettingsEntity is already in the settingsTypes dictionary.  If not, then add it
        if (!jsonConnectionEx.settingsTypes.ContainsKey(typeof(TSettingsEntity).Name))
            jsonConnectionEx.settingsTypes.Add(typeof(TSettingsEntity).Name, typeof(TSettingsEntity));

        return base.Get<TSettingsEntity>();
    }

    public override void Update<TSettingsEntity>(TSettingsEntity settings)
    {
        //Check if this TSettingsEntity is already in the settingsTypes dictionary.  If not, then add it
        if (!jsonConnectionEx.settingsTypes.ContainsKey(typeof(TSettingsEntity).Name))
            jsonConnectionEx.settingsTypes.Add(typeof(TSettingsEntity).Name, typeof(TSettingsEntity));

        base.Update(settings);
    }
}
