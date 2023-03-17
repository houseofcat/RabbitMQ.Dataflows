using System;
using System.IO;
using System.Threading.Tasks;

namespace HouseofCat.Encryption.Providers;

public interface IEncryptionProvider
{
    string Type { get; }
    ArraySegment<byte> Decrypt(ReadOnlyMemory<byte> data);
    MemoryStream Decrypt(MemoryStream data, bool leaveStreamOpen = false);
    MemoryStream DecryptToStream(ReadOnlyMemory<byte> data);
    ArraySegment<byte> Encrypt(ReadOnlyMemory<byte> data);
    MemoryStream Encrypt(MemoryStream data, bool leaveStreamOpen = false);
    Task<MemoryStream> EncryptAsync(MemoryStream data, bool leaveStreamOpen = false);
    MemoryStream EncryptToStream(ReadOnlyMemory<byte> data);
}
