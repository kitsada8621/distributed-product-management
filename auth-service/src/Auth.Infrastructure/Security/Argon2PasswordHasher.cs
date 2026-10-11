using System.Security.Cryptography;
using System.Text;
using Auth.Application.Security;
using Konscious.Security.Cryptography;

namespace Auth.Infrastructure.Security;

public sealed class Argon2PasswordHasher : IPasswordHasher
{
    private const string Prefix = "argon2id";
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int MemoryKiB = 19456;
    private const int Iterations = 2;
    private const int Parallelism = 1;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Compute(password, salt, MemoryKiB, Iterations, Parallelism);
        return $"{Prefix}$m={MemoryKiB},t={Iterations},p={Parallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hash)
    {
        if (!TryParse(hash, out var stored))
        {
            return false;
        }

        var actual = Compute(password, stored.Salt, stored.MemoryKiB, stored.Iterations, stored.Parallelism);
        return CryptographicOperations.FixedTimeEquals(actual, stored.Hash);
    }

    private static byte[] Compute(string password, byte[] salt, int memoryKiB, int iterations, int parallelism)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memoryKiB,
            Iterations = iterations,
            DegreeOfParallelism = parallelism
        };
        return argon2.GetBytes(HashSize);
    }

    private static bool TryParse(string encoded, out StoredHash stored)
    {
        stored = default!;
        var parts = encoded.Split('$');
        if (parts.Length != 4 || parts[0] != Prefix)
        {
            return false;
        }

        var settings = parts[1].Split(',');
        if (settings.Length != 3
            || !TryReadSetting(settings[0], "m=", out var memoryKiB)
            || !TryReadSetting(settings[1], "t=", out var iterations)
            || !TryReadSetting(settings[2], "p=", out var parallelism))
        {
            return false;
        }

        var salt = new byte[SaltSize];
        var hash = new byte[HashSize];
        if (!Convert.TryFromBase64String(parts[2], salt, out var saltLength) || saltLength != SaltSize
            || !Convert.TryFromBase64String(parts[3], hash, out var hashLength) || hashLength != HashSize)
        {
            return false;
        }

        stored = new StoredHash(memoryKiB, iterations, parallelism, salt, hash);
        return true;
    }

    private static bool TryReadSetting(string text, string key, out int value)
    {
        value = 0;
        return text.StartsWith(key, StringComparison.Ordinal)
            && int.TryParse(text.AsSpan(key.Length), out value)
            && value > 0;
    }

    private sealed record StoredHash(int MemoryKiB, int Iterations, int Parallelism, byte[] Salt, byte[] Hash);
}
