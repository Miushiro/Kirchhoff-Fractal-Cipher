using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LicenseGenerator.Encryption.KirchhoffFractals
{
	public static class Boxing
	{
		public static byte[] GenerateDefaultSBox(byte[] masterKey)
		{
			const int size = 256;

			// Initialize as int[] for homomorphic arithmetic
			using (var sha256 = SHA256.Create())
			{
				byte[] seed = sha256.ComputeHash(masterKey);
				int seedInt = BitConverter.ToInt32(seed, 0);
				Random rng = new Random(seedInt);
				int[] sboxInt = new int[size];
				for (int i = 0; i < size; i++)
				{
					sboxInt[i] = i;
				}

				// Homomorphic butterfly shuffle
				//Random rng = new Random();
				rng.HomophorbicButterflyShuffle(sboxInt);

				// Convert to byte[] for output
				byte[] sbox = new byte[size];
				for (int i = 0; i < size; i++)
				{
					sbox[i] = (byte)sboxInt[i];
				}

				return sbox;
			}
		}

		//Deterministic SBox
		public static byte[] GenerateDeterministicSBox(byte[] masterKey)
		{
			const int size = 256;

			// Hash key to get deterministic seed
			byte[] hash = new byte[32];
			using (var sha = System.Security.Cryptography.SHA256.Create())
			{
				hash = sha.ComputeHash(masterKey);
			}

			int seed = BitConverter.ToInt32(hash, 0);

			// Initialize S-box
			int[] sboxInt = new int[size];
			for (int i = 0; i < size; i++)
				sboxInt[i] = i;

			// Use key material for butterfly swaps (not Random!)
			Shuffles.KirchhoffButterfly(sboxInt, hash, 0);

			// Convert to byte array
			byte[] sbox = new byte[size];
			for (int i = 0; i < size; i++)
				sbox[i] = (byte)sboxInt[i];

			return sbox;
		}
	}
}
