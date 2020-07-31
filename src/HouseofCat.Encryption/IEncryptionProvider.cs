using System;
using System.Threading.Tasks;
using static HouseofCat.Encryption.Enums;

namespace HouseofCat.Encryption
{
    public interface IEncryptionProvider
    {
        byte[] Decrypt(ReadOnlyMemory<byte> data, EncryptionMethod method);
        byte[] Encrypt(ReadOnlyMemory<byte> data, EncryptionMethod method);
    }
}
