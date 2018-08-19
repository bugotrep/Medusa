using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Medusa
{
	public static class Cipher
	{
		// This constant is used to determine the keysize of the encryption algorithm in bits.
		// We divide this by 8 within the code below to get the equivalent number of bytes.
		private const int Keysize = 256;

		// This constant determines the number of iterations for the password bytes generation function.
		private const int DerivationIterations = 1000;

		public static CryptoStream Encrypt(Stream stream, string passPhrase)
        {
            // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
            // so that the same Salt and IV values can be used when decrypting.  
            var saltStringBytes = GenerateKeySizeBitsOfRandomEntropy();
            var ivStringBytes = GenerateKeySizeBitsOfRandomEntropy();
            using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
            {
                var keyBytes = password.GetBytes(Keysize / 8);
                using (var symmetricKey = new RijndaelManaged
                {
                    BlockSize = 256,
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.PKCS7
                })
                {
                    var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes);
                    var cryptoStream = new CryptoStream(stream, encryptor, CryptoStreamMode.Write);
                    var cipherTextBytes = saltStringBytes;
                    cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                    stream.Write(cipherTextBytes, 0, cipherTextBytes.Length);
                    return cryptoStream;
                }
            }
        }

        public static CryptoStream Decrypt(Stream stream, string passPhrase)
        {
            var saltStringBytes = new byte[Keysize >> 3];
            stream.Read(saltStringBytes, 0, saltStringBytes.Length);
            var ivStringBytes = new byte[Keysize >> 3];
            stream.Read(ivStringBytes, 0, ivStringBytes.Length);

            using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
            {
                var keyBytes = password.GetBytes(Keysize / 8);
                using (var symmetricKey = new RijndaelManaged
                {
                    BlockSize = 256,
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.PKCS7
                })
                {
                    var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes);
                    var cryptoStream = new CryptoStream(stream, decryptor, CryptoStreamMode.Read);
                    return cryptoStream;
                }
            }
        }

        private static byte[] GenerateKeySizeBitsOfRandomEntropy()
		{
			var randomBytes = new byte[Keysize >> 3];
			using(var rngCsp = new RNGCryptoServiceProvider())
			{
				// Fill the array with cryptographically secure random bytes.
				rngCsp.GetBytes(randomBytes);
			}
			return randomBytes;
		}
	}
}
