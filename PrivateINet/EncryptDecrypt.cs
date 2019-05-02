using System.Security.Cryptography;
using System.IO;
using RSACryptoServiceProviderExtensions;
namespace EncryptDecrypt
{
    public enum KeySizes
    {
        SIZE_512 = 512,
        SIZE_952 = 952,
        SIZE_1369 = 1369,
        SIZE_1024 = 1024,
        SIZE_2048 = 2048,
    };

    class RSA
    {
        public static string publicKey = "./pub.cert";
        public static string privateKey = "./priv.cert";
        public static void GenerateKeys(string publicKeyFile, string privateKeyFile)
        {
            using (var rsa = new RSACryptoServiceProvider((int)KeySizes.SIZE_2048))
            {
                rsa.PersistKeyInCsp = false;

                if (File.Exists(privateKeyFile))
                    File.Delete(privateKeyFile);

                if (File.Exists(publicKeyFile))
                    File.Delete(publicKeyFile);

                string publicKey = RSAExtensions.ToXmlString(rsa, false);
                File.WriteAllText(publicKeyFile, publicKey);
                string privateKey = RSAExtensions.ToXmlString(rsa, true);
                File.WriteAllText(privateKeyFile, privateKey);
            }
        }

        public static byte[] Encrypt(string publicKeyFile, byte[] plain)
        {
            byte[] encrypted;
            using (var rsa = new RSACryptoServiceProvider((int)KeySizes.SIZE_2048))
            {
                rsa.PersistKeyInCsp = false;
                string publicKey = File.ReadAllText(publicKeyFile);

                RSAExtensions.FromXmlString(rsa, publicKey);
                encrypted = rsa.Encrypt(plain, true);
            }

            return encrypted;
        }

        public static byte[] Decrypt(string privateKeyFile, byte[] encrypted)
        {
            byte[] decrypted;
            using (var rsa = new RSACryptoServiceProvider((int)KeySizes.SIZE_2048))
            {
                rsa.PersistKeyInCsp = false;
                string privateKey = File.ReadAllText(privateKeyFile);
                RSAExtensions.FromXmlString(rsa, privateKey);
                decrypted = rsa.Decrypt(encrypted, true);
            }

            return decrypted;
        }
    }
}


