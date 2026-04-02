using System.Security.Cryptography;
using System.Text;

namespace GridAcademy.Services.ExamContent.Utilities;

public static class HashHelper
{
    public static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
