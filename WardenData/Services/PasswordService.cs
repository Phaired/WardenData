using System.Security.Cryptography;
using System.Text;

namespace WardenData.Services;

public class PasswordService : IPasswordService
{
    public string HashPassword(string password)
    {
        // Utilise BCrypt pour un hachage sécurisé
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}