using System;
using System.Threading.Tasks;
using static HouseofCat.Encryption.Enums;

namespace HouseofCat.Encryption
{
    public interface IEncryptionProvider
    {
        string Type { get; }
        byte[] Decrypt(ReadOnlyMemory<byte> data);
        byte[] Encrypt(ReadOnlyMemory<byte> data);
    }
}
