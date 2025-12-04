using Encryption.KirchhoffFractals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AsiasoftPluginLicense.KirchoffFractals
{
	public static class Cipher
	{
		private static string Decrypt(string base64Cipher, byte[] seed, int block = 32, int rounds = 128)
		{
			Console.WriteLine(base64Cipher);
			byte[] data = Convert.FromBase64String(base64Cipher);
			byte[] DefaultSBox = KirchhoffFeistelCipherNve.GenerateDeterministicSBox(seed);
			for (int round = rounds - 1; round >= 0; round--)
			{
				byte[] roundKey = KirchhoffFeistelCipherNve.Key(seed, rounds, block);
				KirchhoffFeistelCipherNve.DecryptRound(data, roundKey, DefaultSBox);
			}
			int trimmedLength = data.Length;
			while (trimmedLength > 0 && data[trimmedLength - 1] == 0)
				trimmedLength--;

			byte[] unpadded = new byte[trimmedLength];
			Console.WriteLine(Convert.ToBase64String(data));
			Buffer.BlockCopy(data, 0, unpadded, 0, trimmedLength);
			string primed = Encoding.UTF8.GetString(unpadded);
			Console.WriteLine(primed);
			return primed;
		}
	}
}
