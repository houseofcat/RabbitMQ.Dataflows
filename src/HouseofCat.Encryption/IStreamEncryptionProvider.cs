using System.IO;
using System.Security.Cryptography;

namespace HouseofCat.Encryption
{
    public interface IStreamEncryptionProvider
    {
        string Type { get; }
        CryptoStream GetEncryptStream(Stream stream, bool leaveOpen = false);
        CryptoStream GetDecryptStream(Stream stream, bool leaveOpen = false);
    }
}
