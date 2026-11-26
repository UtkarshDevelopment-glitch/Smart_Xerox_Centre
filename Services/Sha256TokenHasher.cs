using System.Security.Cryptography;
using System.Text;

namespace Smart_Xerox_Centre.Services;

public sealed class Sha256TokenHasher : ITokenHasher
{
    public string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
