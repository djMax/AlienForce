using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace AlienForce.Utilities.Security
{
	/// <summary>
	/// Symmetric encryption (AES[CBC]+HMAC) class. NOT THREAD SAFE for encryption (signing ok).  Lock it or keep it single threaded.
	/// 
	/// Encryption Format: HMACSHA256 Hash, IV, CipherText
	/// </summary>
	public class CryptKeeper
	{
		Aes Cryptor;
		HMACSHA256 Signer;

		int MacSize;
		int BlockSize;
		int CipherStart;

		/// <summary>
		/// Reusable byte array for encryption
		/// </summary>
		byte[] IV;

		public CryptKeeper(byte[] key)
		{
			Cryptor = AesCryptoServiceProvider.Create();
			Cryptor.Key = key;
			Signer = (HMACSHA256)new HMACSHA256(key);
			_Init();
		}

		private void _Init()
		{
			Cryptor.Mode = CipherMode.CBC;
			Cryptor.Padding = PaddingMode.PKCS7;
			MacSize = Signer.HashSize / 8;
			BlockSize = Cryptor.BlockSize / 8;
			IV = new byte[BlockSize];
			CipherStart = MacSize + BlockSize;
		}

		public byte[] Encrypt(string str)
		{
			var b = Encoding.UTF8.GetBytes(str);
			return Encrypt(b, 0, b.Length);
		}

		public byte[] Encrypt(byte[] buffer, int offset, int inLen)
		{
			int i, j = CipherStart, len;
			Cryptor.GenerateIV();

			ICryptoTransform ct = Cryptor.CreateEncryptor();
			byte[] xformed = ct.TransformFinalBlock(buffer, offset, inLen);

			// Data size + BlockSize byte IV + 16 byte HMAC
			byte[] aesOutLwEnc = new byte[xformed.Length + CipherStart];

			var iv = Cryptor.IV;
			for (i = 0; i < BlockSize; i++)
			{
				aesOutLwEnc[i + MacSize] = iv[i];
			}
			for (i = 0, len = xformed.Length; i < len; i++)
			{
				aesOutLwEnc[j++] = xformed[i];
			}

			// Generate the HMAC
			byte[] sig = Signer.ComputeHash(aesOutLwEnc, MacSize, aesOutLwEnc.Length - MacSize);
			for (i = 0; i < MacSize; i++)
			{
				aesOutLwEnc[i] = sig[i];
			}

			return aesOutLwEnc;
		}

		public byte[] Sign(string q)
		{
			return Signer.ComputeHash(Encoding.UTF8.GetBytes(q));
		}

		public byte[] Sign(byte[] b)
		{
			return Signer.ComputeHash(b);
		}

		public byte[] Encrypt(byte[] buffer)
		{
			return Encrypt(buffer, 0, buffer.Length);
		}

		public byte[] Decrypt(byte[] buffer, int offset, int inLen)
		{
			if (inLen <= CipherStart)
			{
				return null; // bad message.
			}
			byte[] sig = Signer.ComputeHash(buffer, MacSize + offset, inLen - MacSize);

			for (var j = MacSize - 1; j >= 0; j--)
			{
				if (sig[j] != buffer[j + offset])
				{
					return null; // HMAC Fail
				}
			}

			// Copy the IV
			int i;
			for (i = 0; i < BlockSize; i++) { IV[i] = buffer[i + offset + MacSize]; }

			Cryptor.IV = IV;
			var decryptor = Cryptor.CreateDecryptor();
			return decryptor.TransformFinalBlock(buffer, offset + CipherStart, inLen - CipherStart);
		}

		public byte[] Decrypt(byte[] buffer)
		{
			return Decrypt(buffer, 0, buffer.Length);
		}
	}
}
