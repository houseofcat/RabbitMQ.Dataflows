using System;

namespace HouseofCat.Encryption
{
    public interface IEncryptionProvider
    {
        string Type { get; }
        byte[] Decrypt(ReadOnlyMemory<byte> data);
        byte[] Encrypt(ReadOnlyMemory<byte> data);
    }
}
