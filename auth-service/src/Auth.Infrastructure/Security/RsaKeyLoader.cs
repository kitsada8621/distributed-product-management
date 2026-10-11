using System.Security.Cryptography;

namespace Auth.Infrastructure.Security;

public static class RsaKeyLoader
{
    public static RSA Load(string privateKeyPath)
    {
        if (!File.Exists(privateKeyPath))
        {
            throw new FileNotFoundException($"JWT private key file not found: {privateKeyPath}", privateKeyPath);
        }

        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(privateKeyPath));
        return rsa;
    }
}
