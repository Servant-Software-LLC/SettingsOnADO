using Microsoft.AspNetCore.DataProtection;

namespace SettingsOnADO;

public class DataProtectionEncryptionProvider : IEncryptionProvider
{
    private readonly IDataProtector _protector;

    public DataProtectionEncryptionProvider(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("SettingsOnADO.Encryption");
    }

    public string Encrypt(string plainText)
    {
        return _protector.Protect(plainText);
    }

    public string Decrypt(string cipherText)
    {
        return _protector.Unprotect(cipherText);
    }
}
