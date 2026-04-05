using System.Security.Cryptography;
using Xunit;

namespace SettingsOnADO.Tests;

public class AesEncryptionProviderTests
{
    private static byte[] GenerateKey(int bits = 256)
    {
        var key = new byte[bits / 8];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip_ReturnsOriginalPlaintext()
    {
        var provider = new AesEncryptionProvider(GenerateKey());
        var plaintext = "Hello, World!";

        var ciphertext = provider.Encrypt(plaintext);
        var result = provider.Decrypt(ciphertext);

        Assert.Equal(plaintext, result);
    }

    [Fact]
    public void Encrypt_DifferentPlaintexts_ProduceDifferentCiphertexts()
    {
        var provider = new AesEncryptionProvider(GenerateKey());

        var cipher1 = provider.Encrypt("plaintext1");
        var cipher2 = provider.Encrypt("plaintext2");

        Assert.NotEqual(cipher1, cipher2);
    }

    [Fact]
    public void Encrypt_SamePlaintext_ProducesDifferentCiphertexts()
    {
        // Because a random IV is generated per call
        var provider = new AesEncryptionProvider(GenerateKey());
        var plaintext = "same text";

        var cipher1 = provider.Encrypt(plaintext);
        var cipher2 = provider.Encrypt(plaintext);

        Assert.NotEqual(cipher1, cipher2);
    }

    [Fact]
    public void EncryptDecrypt_EmptyString()
    {
        var provider = new AesEncryptionProvider(GenerateKey());

        var ciphertext = provider.Encrypt(string.Empty);
        var result = provider.Decrypt(ciphertext);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void EncryptDecrypt_UnicodeAndSpecialCharacters()
    {
        var provider = new AesEncryptionProvider(GenerateKey());
        var plaintext = "Ünïcödé 日本語 🔑 <script>alert('xss')</script>";

        var ciphertext = provider.Encrypt(plaintext);
        var result = provider.Decrypt(ciphertext);

        Assert.Equal(plaintext, result);
    }

    [Fact]
    public void Decrypt_InvalidBase64_ThrowsFormatException()
    {
        var provider = new AesEncryptionProvider(GenerateKey());

        Assert.Throws<FormatException>(() => provider.Decrypt("not-valid-base64!!!"));
    }

    [Fact]
    public void Decrypt_CorruptedCiphertext_ThrowsCryptographicException()
    {
        var provider = new AesEncryptionProvider(GenerateKey());
        var ciphertext = provider.Encrypt("test");

        // Corrupt the ciphertext by modifying bytes after the IV
        var bytes = Convert.FromBase64String(ciphertext);
        bytes[bytes.Length - 1] ^= 0xFF;
        var corrupted = Convert.ToBase64String(bytes);

        Assert.ThrowsAny<CryptographicException>(() => provider.Decrypt(corrupted));
    }

    [Fact]
    public void Constructor_InvalidKeySize_ThrowsCryptographicException()
    {
        var badKey = new byte[10]; // AES requires 16, 24, or 32 byte keys
        var provider = new AesEncryptionProvider(badKey);

        Assert.ThrowsAny<CryptographicException>(() => provider.Encrypt("test"));
    }

    [Theory]
    [InlineData(128)]
    [InlineData(192)]
    [InlineData(256)]
    public void EncryptDecrypt_ValidKeySizes(int keyBits)
    {
        var provider = new AesEncryptionProvider(GenerateKey(keyBits));
        var plaintext = "test with different key sizes";

        var ciphertext = provider.Encrypt(plaintext);
        var result = provider.Decrypt(ciphertext);

        Assert.Equal(plaintext, result);
    }

#pragma warning disable CS0618 // Suppress obsolete warning for testing the obsolete constructor
    [Fact]
    public void ObsoleteConstructor_IgnoresIvParameter_StillWorks()
    {
        var key = GenerateKey();
        var iv = new byte[16];
        RandomNumberGenerator.Fill(iv);

        var provider = new AesEncryptionProvider(key, iv);
        var plaintext = "test obsolete constructor";

        var ciphertext = provider.Encrypt(plaintext);
        var result = provider.Decrypt(ciphertext);

        Assert.Equal(plaintext, result);
    }
#pragma warning restore CS0618

    [Fact]
    public void EncryptDecrypt_LongString()
    {
        var provider = new AesEncryptionProvider(GenerateKey());
        var plaintext = new string('A', 10_000);

        var ciphertext = provider.Encrypt(plaintext);
        var result = provider.Decrypt(ciphertext);

        Assert.Equal(plaintext, result);
    }

    [Fact]
    public void Decrypt_WithWrongKey_ThrowsCryptographicException()
    {
        var provider1 = new AesEncryptionProvider(GenerateKey());
        var provider2 = new AesEncryptionProvider(GenerateKey());

        var ciphertext = provider1.Encrypt("secret");

        Assert.ThrowsAny<CryptographicException>(() => provider2.Decrypt(ciphertext));
    }
}
