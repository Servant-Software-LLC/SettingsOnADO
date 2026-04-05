using Microsoft.AspNetCore.DataProtection;
using Moq;
using Xunit;

namespace SettingsOnADO.Tests;

public class DataProtectionEncryptionProviderTests
{
    private static DataProtectionEncryptionProvider CreateProvider()
    {
        var dpProvider = new EphemeralDataProtectionProvider();
        return new DataProtectionEncryptionProvider(dpProvider);
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip_ReturnsOriginalPlaintext()
    {
        var provider = CreateProvider();
        var plaintext = "Hello, World!";

        var ciphertext = provider.Encrypt(plaintext);
        var result = provider.Decrypt(ciphertext);

        Assert.Equal(plaintext, result);
    }

    [Fact]
    public void Encrypt_ProducesDifferentOutputThanInput()
    {
        var provider = CreateProvider();
        var plaintext = "sensitive data";

        var ciphertext = provider.Encrypt(plaintext);

        Assert.NotEqual(plaintext, ciphertext);
    }

    [Fact]
    public void EncryptDecrypt_EmptyString()
    {
        var provider = CreateProvider();

        var ciphertext = provider.Encrypt(string.Empty);
        var result = provider.Decrypt(ciphertext);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void EncryptDecrypt_UnicodeAndSpecialCharacters()
    {
        var provider = CreateProvider();
        var plaintext = "Ünïcödé 日本語 🔑 <script>alert('xss')</script>";

        var ciphertext = provider.Encrypt(plaintext);
        var result = provider.Decrypt(ciphertext);

        Assert.Equal(plaintext, result);
    }

    [Fact]
    public void Decrypt_InvalidCiphertext_Throws()
    {
        var provider = CreateProvider();

        Assert.ThrowsAny<Exception>(() => provider.Decrypt("not-a-valid-protected-payload"));
    }

    [Fact]
    public void Constructor_UsesCorrectPurpose()
    {
        // Verify that the provider creates a protector with the expected purpose string
        var mockProtector = new Mock<IDataProtector>();
        var mockProvider = new Mock<IDataProtectionProvider>();
        mockProvider
            .Setup(p => p.CreateProtector("SettingsOnADO.Encryption"))
            .Returns(mockProtector.Object);

        var provider = new DataProtectionEncryptionProvider(mockProvider.Object);

        mockProvider.Verify(p => p.CreateProtector("SettingsOnADO.Encryption"), Times.Once);
    }

    [Fact]
    public void Encrypt_DelegatesTo_Protect()
    {
        var mockProtector = new Mock<IDataProtector>();
        mockProtector.Setup(p => p.Protect(It.IsAny<byte[]>()))
            .Returns<byte[]>(input => input); // pass-through for the mock
        var mockProvider = new Mock<IDataProtectionProvider>();
        mockProvider
            .Setup(p => p.CreateProtector(It.IsAny<string>()))
            .Returns(mockProtector.Object);

        var provider = new DataProtectionEncryptionProvider(mockProvider.Object);
        provider.Encrypt("test");

        // Verify Protect was called (the IDataProtector.Protect extension calls the byte[] overload)
        mockProtector.Verify(p => p.Protect(It.IsAny<byte[]>()), Times.Once);
    }

    [Fact]
    public void Decrypt_DelegatesTo_Unprotect()
    {
        var mockProtector = new Mock<IDataProtector>();
        mockProtector.Setup(p => p.Unprotect(It.IsAny<byte[]>()))
            .Returns<byte[]>(input => input); // pass-through for the mock
        var mockProvider = new Mock<IDataProtectionProvider>();
        mockProvider
            .Setup(p => p.CreateProtector(It.IsAny<string>()))
            .Returns(mockProtector.Object);

        var provider = new DataProtectionEncryptionProvider(mockProvider.Object);
        provider.Decrypt("dGVzdA=="); // base64 of "test"

        mockProtector.Verify(p => p.Unprotect(It.IsAny<byte[]>()), Times.Once);
    }

    [Fact]
    public void EncryptDecrypt_LongString()
    {
        var provider = CreateProvider();
        var plaintext = new string('B', 10_000);

        var ciphertext = provider.Encrypt(plaintext);
        var result = provider.Decrypt(ciphertext);

        Assert.Equal(plaintext, result);
    }
}
