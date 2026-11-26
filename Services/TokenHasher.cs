
using System.Security.Cryptography;
using System.Text;

namespace Smart_Xerox_Centre.Services;

public class TokenHasher : ITokenHasher
{
    public string Hash(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Token value cannot be empty.");
        }

        byte[] hashBytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(value)
        );

        return Convert.ToHexString(hashBytes);
    }
}
