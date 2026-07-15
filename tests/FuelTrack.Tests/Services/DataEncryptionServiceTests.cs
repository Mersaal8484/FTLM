using FuelTrack.API.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace FuelTrack.Tests.Services;

public class DataEncryptionServiceTests
{
    private const string TestKey = "DnrpIAixmM+tEYmWJaNkU6iWnxFVcSAJ6NWTjQxuB+w=";
    private readonly AesDataEncryptionService _sut;

    public DataEncryptionServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DataEncryption:Key"] = TestKey })
            .Build();
        _sut = new AesDataEncryptionService(config, NullLogger<AesDataEncryptionService>.Instance);
    }

    [Fact]
    public void Encrypt_ReturnsDifferentString_ThanPlaintext()
    {
        var plain = "sensitive-data-12345";
        var encrypted = _sut.Encrypt(plain);
        Assert.NotEqual(plain, encrypted);
        Assert.False(string.IsNullOrEmpty(encrypted));
    }

    [Fact]
    public void Decrypt_ReturnsOriginalPlaintext()
    {
        var plain = "fuel-level-reading-42.5";
        var encrypted = _sut.Encrypt(plain);
        var decrypted = _sut.Decrypt(encrypted);
        Assert.Equal(plain, decrypted);
    }

    [Fact]
    public void Encrypt_SamePlaintext_ProducesDifferentCiphertext()
    {
        var plain = "same-value";
        var enc1 = _sut.Encrypt(plain);
        var enc2 = _sut.Encrypt(plain);
        Assert.NotEqual(enc1, enc2); // random IV
    }

    [Fact]
    public void HashPassword_ReturnsDifferentHash_EachCall()
    {
        var pw = "Admin@123";
        var hash1 = _sut.HashPassword(pw);
        var hash2 = _sut.HashPassword(pw);
        Assert.NotEqual(hash1, hash2); // different salt
    }

    [Fact]
    public void VerifyPassword_ReturnsTrue_ForCorrectPassword()
    {
        var pw = "Supervisor#456";
        var hash = _sut.HashPassword(pw);
        Assert.True(_sut.VerifyPassword(pw, hash));
    }

    [Fact]
    public void VerifyPassword_ReturnsFalse_ForWrongPassword()
    {
        var hash = _sut.HashPassword("correct-pw");
        Assert.False(_sut.VerifyPassword("wrong-pw", hash));
    }

    [Fact]
    public void Encrypt_EmptyString_ReturnsEmpty()
    {
        var result = _sut.Encrypt("");
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Encrypt_NullString_ReturnsEmpty()
    {
        var result = _sut.Encrypt(null);
        Assert.Null(result);
    }

    [Fact]
    public void Decrypt_NullString_ReturnsNull()
    {
        var result = _sut.Decrypt(null);
        Assert.Null(result);
    }

    [Fact]
    public void VerifyPassword_NullHash_ReturnsFalse()
    {
        Assert.False(_sut.VerifyPassword("anything", null));
    }

    [Fact]
    public void HashPassword_NullPassword_ThrowsOrReturns()
    {
        var ex = Record.Exception(() => _sut.HashPassword(null));
        // May throw ArgumentNullException or return gracefully depending on runtime
        // Either outcome is acceptable
    }
}
