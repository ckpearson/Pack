using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Pack_v2.Security
{
    public struct PackKey
    {
        public byte[] PublicKey { get; }
        public byte[] EncryptedPrivateKey { get; }
        public byte[] InitialisationVector { get; }
        public byte[] Salt { get; }

        public byte[] Challenge { get; }

        public PackKey(byte[] publicKey, byte[] encryptedPrivateKey, byte[] initialisationVector, byte[] salt, byte[] challenge)
        {
            PublicKey = publicKey;
            EncryptedPrivateKey = encryptedPrivateKey;
            InitialisationVector = initialisationVector;
            Salt = salt;
            Challenge = challenge;
        }
    }

    public struct SecureData
    {
        public byte[] EncryptedData { get;  }
        public byte[] Signature { get; }
        public byte[] OriginatorPublicKey { get;  }
        public byte[] AccessKey { get; }

        public SecureData(byte[] encryptedData, byte[] signature, byte[] originatorPublicKey, byte[] accessKey)
        {
            EncryptedData = encryptedData;
            Signature = signature;
            OriginatorPublicKey = originatorPublicKey;
            AccessKey = accessKey;
        }
    }

    public static class Encryption
    {
        private const int RfcIterationCount = 10000;
        private const int RsaKeySize = 3072;
        private const int AesKeySize = 256;
        private static readonly byte[] EmptyAesIv = Enumerable.Repeat((byte) 0, 16).ToArray();

        private static RSACryptoServiceProvider GetRsa()
        {
            var rsa = new RSACryptoServiceProvider(RsaKeySize) {PersistKeyInCsp = false};
            return rsa;
        }

        private static Rfc2898DeriveBytes GetRfc(string pass, byte[] salt)
        {
            return new Rfc2898DeriveBytes(pass, salt, RfcIterationCount);
        }

        private static AesCryptoServiceProvider GetAes()
        {
            var aes = new AesCryptoServiceProvider {KeySize = AesKeySize};
            return aes;
        }

        public static SecureData SecureData(byte[] data, string pass, PackKey originatorKey)
        {
            var encryptionKey = NewEncryptionKey();
            var encryptedData = AesEncrypt(encryptionKey, data);
            var originatorPrivateKey = DecryptPrivateKey(pass, originatorKey.Salt,
                originatorKey.InitialisationVector,
                originatorKey.EncryptedPrivateKey);

            var signature = RsaSign(originatorPrivateKey, encryptedData);
            return new SecureData(encryptedData, signature, originatorKey.PublicKey, encryptionKey);
        }

        
        public static PackKey DerivePackKey(string pass, byte[] salt)
        {
            using (var rsa = GetRsa())
            {
                var publicKey = rsa.ExportCspBlob(false);
                var privateKey = rsa.ExportCspBlob(true);

                using (var rfc = GetRfc(pass, salt))
                {
                    using (var aes = GetAes())
                    {
                        aes.Key = rfc.GetBytes(aes.KeySize/8);
                        aes.GenerateIV();

                        using (var enc = aes.CreateEncryptor(aes.Key, aes.IV))
                        {
                            using (var ms = new MemoryStream())
                            {
                                using (var cs = new CryptoStream(ms, enc, CryptoStreamMode.Write))
                                {
                                    cs.Write(privateKey, 0, privateKey.Length);
                                    cs.FlushFinalBlock();
                                }

                                return new PackKey(
                                    publicKey,
                                    ms.ToArray(),
                                    aes.IV,
                                    salt,
                                    RsaEncrypt(publicKey, salt));
                            }
                        }
                    }
                }
            }
        }

        public static byte[] NewEncryptionKey()
        {
            using (var aes = GetAes())
            {
                aes.IV = EmptyAesIv;
                aes.GenerateKey();
                return aes.Key;
            }
        }

        public static byte[] AesEncrypt(byte[] key, byte[] data)
        {
            using (var aes = GetAes())
            {
                aes.IV = EmptyAesIv;
                aes.Key = key;

                using (var enc = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, enc, CryptoStreamMode.Write))
                        {
                            cs.Write(data, 0, data.Length);
                            cs.FlushFinalBlock();
                        }
                        return ms.ToArray();
                    }
                }
            }
        }

        public static byte[] AesDecrypt(byte[] key, byte[] data)
        {
            using (var aes = GetAes())
            {
                aes.IV = EmptyAesIv;
                aes.Key = key;

                using (var dec = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    using (var outputMs = new MemoryStream())
                    {
                        using (var inputMs = new MemoryStream(data))
                        {
                            using (var cs = new CryptoStream(inputMs, dec, CryptoStreamMode.Read))
                            {
                                var buffer = new byte[1024];
                                var read = cs.Read(buffer, 0, buffer.Length);
                                while (read > 0)
                                {
                                    outputMs.Write(buffer, 0, read);
                                    read = cs.Read(buffer, 0, buffer.Length);
                                }
                                cs.Flush();
                            }
                            inputMs.Flush();
                        }
                        return outputMs.ToArray();
                    }
                }
            }
        }

        public static byte[] RsaSign(byte[] privateKey, byte[] data)
        {
            using (var rsa = GetRsa())
            {
                rsa.ImportCspBlob(privateKey);
                using (var sha = new SHA256Managed())
                {
                    return rsa.SignData(data, sha);
                }
            }
        }

        public static bool RsaVerify(byte[] publicKey, byte[] signature, byte[] data)
        {
            using (var rsa = GetRsa())
            {
                rsa.ImportCspBlob(publicKey);
                using (var sha = new SHA256Managed())
                {
                    return rsa.VerifyData(data, sha, signature);
                }
            }
        }

        public static byte[] DecryptPrivateKey(string pass, byte[] salt, byte[] iv, byte[] encryptedPrivateKey)
        {
            using (var rfc = GetRfc(pass, salt))
            {
                using (var aes = GetAes())
                {
                    aes.Key = rfc.GetBytes(aes.KeySize/8);
                    aes.IV = iv;

                    using (var dec = aes.CreateDecryptor(aes.Key, aes.IV))
                    {
                        using (var ms = new MemoryStream(encryptedPrivateKey))
                        {
                            using (var cs = new CryptoStream(ms, dec, CryptoStreamMode.Read))
                            {
                                var decryptedKey = new byte[RsaKeySize];
                                cs.Read(decryptedKey, 0, RsaKeySize);
                                return decryptedKey;
                            }
                        }
                    }
                }
            }
        }

        public static byte[] RsaEncrypt(byte[] publicKey, byte[] data)
        {
            using (var rsa = GetRsa())
            {
                rsa.ImportCspBlob(publicKey);
                return rsa.Encrypt(data, true);
            }
        }

        public static byte[] RsaDecrypt(byte[] privateKey, byte[] data)
        {
            using (var rsa = GetRsa())
            {
                rsa.ImportCspBlob(privateKey);
                return rsa.Decrypt(data, true);
            }
        }
    }
}