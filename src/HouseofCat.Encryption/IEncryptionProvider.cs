using System;

namespace HouseofCat.Encryption
{
    public interface IEncryptionProvider
    {
        string Type { get; }
        ArraySegment<byte> Decrypt(ReadOnlyMemory<byte> data);
        ArraySegment<byte> Encrypt(ReadOnlyMemory<byte> data);
    }
}
