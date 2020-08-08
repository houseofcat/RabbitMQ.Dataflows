using System;
using System.Threading.Tasks;
using static HouseofCat.Encryption.Enums;

namespace HouseofCat.Encryption
{
    public interface IEncryptionProvider
    {
        byte[] Decrypt(ReadOnlyMemory<byte> data);
        byte[] Encrypt(ReadOnlyMemory<byte> data);
    }
}
