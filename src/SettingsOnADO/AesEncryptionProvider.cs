using System.Security.Cryptography;

namespace SettingsOnADO;

public class AesEncryptionProvider : IEncryptionProvider
{
    private readonly byte[] _key;

    public AesEncryptionProvider(byte[] key)
    {
        _key = key;
    }

    [Obsolete("Use the single-parameter constructor AesEncryptionProvider(byte[] key) instead. The IV parameter is ignored; a random IV is generated per encryption operation.")]
    public AesEncryptionProvider(byte[] key, byte[] iv)
    {
        _key = key;
        // iv is intentionally ignored — a random IV is generated per Encrypt call
    }

    public string Encrypt(string plainText)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = _key;
            aes.GenerateIV();
            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using (var ms = new MemoryStream())
            {
                // Prepend the random IV to the ciphertext
                ms.Write(aes.IV, 0, aes.IV.Length);
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    public string Decrypt(string cipherText)
    {
        var fullCipher = Convert.FromBase64String(cipherText);
        using (var aes = Aes.Create())
        {
            aes.Key = _key;
            var ivLength = aes.BlockSize / 8;

            // Extract the IV from the beginning of the ciphertext
            var iv = new byte[ivLength];
            Array.Copy(fullCipher, 0, iv, 0, ivLength);
            aes.IV = iv;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using (var ms = new MemoryStream(fullCipher, ivLength, fullCipher.Length - ivLength))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }
}
