using System;
using System.IO;
using System.Threading.Tasks;

namespace HouseofCat.Encryption
{
    public interface IEncryptionProvider
    {
        string Type { get; }
        ArraySegment<byte> Decrypt(ReadOnlyMemory<byte> data);
        MemoryStream Decrypt(Stream data);
        MemoryStream DecryptToStream(ReadOnlyMemory<byte> data);
        ArraySegment<byte> Encrypt(ReadOnlyMemory<byte> data);
        MemoryStream Encrypt(Stream data);
        Task<MemoryStream> EncryptAsync(Stream data);
        MemoryStream EncryptToStream(ReadOnlyMemory<byte> data);
    }
}
