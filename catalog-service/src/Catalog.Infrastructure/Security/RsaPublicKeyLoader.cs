using System.Security.Cryptography;

namespace Catalog.Infrastructure.Security;

public static class RsaPublicKeyLoader
{
    public static RSA Load(string publicKeyPath)
    {
        if (!File.Exists(publicKeyPath))
        {
            throw new FileNotFoundException($"JWT public key file not found: {publicKeyPath}", publicKeyPath);
        }

        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(publicKeyPath));
        return rsa;
    }
}
