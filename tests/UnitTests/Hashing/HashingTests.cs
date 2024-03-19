using HouseofCat.Hashing;

namespace Hashing;

public class HashingTests
{
    private readonly IHashingProvider _provider;
    private const string _passphrase = "SuperNintendoHadTheBestZelda";
    private const string _salt = "SegaGenesisIsTheBestConsole";

    public HashingTests()
    {
        _provider = new ArgonHashingProvider();
    }

    [Fact]
    public async Task Argon2ID_Hash_256bit()
    {
        var hashKey = await _provider
            .GetHashKeyAsync(_passphrase, _salt, 32);

        Assert.True(hashKey.Length == 32);
    }

    [Fact]
    public async Task Argon2ID_Hash_192bit()
    {
        var hashKey = await _provider
            .GetHashKeyAsync(_passphrase, _salt, 24);

        Assert.True(hashKey.Length == 24);
    }

    [Fact]
    public async Task Argon2ID_Hash_128bit()
    {
        var hashKey = await _provider
            .GetHashKeyAsync(_passphrase, _salt, 16);

        Assert.True(hashKey.Length == 16);
    }

    [Fact]
    public async Task Argon2ID_Hash_NullPassphrase()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _provider.GetHashKeyAsync(null, _salt, 16));
    }

    [Fact]
    public async Task Argon2ID_Hash_NullSalt()
    {
        var hashKey = await _provider.GetHashKeyAsync(_passphrase, salt: null, 16);
        Assert.True(hashKey.Length == 16);
    }

    [Fact]
    public async Task Argon2ID_Hash_EmptyStringSalt()
    {
        var hashKey = await _provider.GetHashKeyAsync(_passphrase, salt: string.Empty, 16);
        Assert.True(hashKey.Length == 16);
    }

    [Fact]
    public async Task Argon2ID_Hash_EmptyByteArrays()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _provider.GetHashKeyAsync(Array.Empty<byte>(), salt: Array.Empty<byte>(), 16));
    }
}
