using System;
using System.IO;

namespace HouseofCat.Encryption
{
    public interface IEncryptionProvider
    {
        string Type { get; }
        ArraySegment<byte> Decrypt(ReadOnlyMemory<byte> data);
        MemoryStream DecryptToStream(ReadOnlyMemory<byte> data);
        ArraySegment<byte> Encrypt(ReadOnlyMemory<byte> data);
        MemoryStream EncryptToStream(ReadOnlyMemory<byte> data);
    }
}
