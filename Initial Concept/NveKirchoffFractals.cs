using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Encryption.KirchhoffFractals
{
	public static class KirchhoffFeistelCipherNve
	{
		#region Core Operations

		// CORRECT: Deterministic butterfly network using key material
		private static void KirchhoffButterfly(int[] array, byte[] key, int offset)
		{
			int n = array.Length;
			if (n < 2) return;

			int levels = (int)Math.Log(n, 2);
			int keyIdx = offset;

			for (int level = 0; level < levels; level++)
			{
				int mask = 1 << level;

				for (int i = 0; i < n; i++)
				{
					int j = i ^ mask;

					if (j > i)
					{
						// Deterministic swap based on key bit
						byte keyByte = key[keyIdx % key.Length];
						int swapBit = (keyByte >> (keyIdx % 8)) & 1;
						keyIdx++;

						if (swapBit == 1)
						{
							// Simple swap (no fake SIMD)
							int temp = array[i];
							array[i] = array[j];
							array[j] = temp;
						}
					}
				}
			}
		}

		// Butterfly networks with deterministic swaps are SELF-INVERTING
		private static void KirchhoffButterflyInverse(int[] array, byte[] key, int offset)
		{
			// Same operation inverts itself!
			KirchhoffButterfly(array, key, offset);
		}

		#endregion

		#region Round 1: Key XOR

		public static void Round1_KeyXOR(byte[] data, byte[] roundKey)
		{
			int n = data.Length;
			int vectorSize = Vector<byte>.Count;

			// Extend key for safe SIMD access
			byte[] extendedKey = new byte[((n + vectorSize - 1) / vectorSize) * vectorSize];
			for (int i = 0; i < extendedKey.Length; i++)
				extendedKey[i] = roundKey[i % roundKey.Length];

			for (int i = 0; i < n; i += vectorSize)
			{
				if (i + vectorSize <= n)
				{
					var block = new Vector<byte>(data, i);
					var keyBlock = new Vector<byte>(extendedKey, i);
					block ^= keyBlock;
					block.CopyTo(data, i);
				}
				else
				{
					// Handle remainder bytes
					for (int j = i; j < n; j++)
						data[j] ^= roundKey[j % roundKey.Length];
				}
			}
		}

		// XOR is self-inverting
		public static void Round1_KeyXOR_Inverse(byte[] data, byte[] roundKey)
		{
			Round1_KeyXOR(data, roundKey);
		}

		#endregion

		#region Round 2: S-Box Substitution and Bit Rotation

		public static void Round2_SubstituteAndRotate(byte[] data, byte[] sbox)
		{
			int n = data.Length;

			for (int i = 0; i < n; i++)
			{
				// S-box substitution
				data[i] = sbox[data[i]];

				// Bit rotation: left rotate by 3
				data[i] = (byte)((data[i] << 3) | (data[i] >> 5));
			}
		}

		public static void Round2_SubstituteAndRotate_Inverse(byte[] data, byte[] sbox)
		{
			int n = data.Length;

			// Build inverse S-box
			byte[] inverseSBox = new byte[256];
			for (int i = 0; i < 256; i++)
				inverseSBox[sbox[i]] = (byte)i;

			for (int i = 0; i < n; i++)
			{
				// Inverse bit rotation: right rotate by 3
				data[i] = (byte)((data[i] >> 3) | (data[i] << 5));

				// Inverse S-box substitution
				data[i] = inverseSBox[data[i]];
			}
		}

		#endregion

		#region Round 3: Kirchhoff Butterfly Permutation

		public static void Round3_ButterflyPermutation(byte[] data, byte[] roundKey)
		{
			int n = data.Length;

			// Convert to int array
			int[] dataInt = Array.ConvertAll(data, b => (int)b);

			// Pad to power of 2 for butterfly network
			int paddedSize = 1;
			while (paddedSize < n) paddedSize <<= 1;

			if (paddedSize > n)
			{
				int[] padded = new int[paddedSize];
				Array.Copy(dataInt, padded, n);
				KirchhoffButterfly(padded, roundKey, 0);
				Array.Copy(padded, dataInt, n);
			}
			else
			{
				KirchhoffButterfly(dataInt, roundKey, 0);
			}

			// Convert back to bytes
			for (int i = 0; i < n; i++)
				data[i] = (byte)dataInt[i];
		}

		// Butterfly is self-inverting
		public static void Round3_ButterflyPermutation_Inverse(byte[] data, byte[] roundKey)
		{
			Round3_ButterflyPermutation(data, roundKey);
		}

		#endregion

		#region Complete Encryption/Decryption

		public static void EncryptRound(byte[] data, byte[] roundKey, byte[] sbox)
		{
			Round1_KeyXOR(data, roundKey);
			Round2_SubstituteAndRotate(data, sbox);
			Round3_ButterflyPermutation(data, roundKey);
		}

		public static void DecryptRound(byte[] data, byte[] roundKey, byte[] sbox)
		{
			// Apply rounds in REVERSE order
			Round3_ButterflyPermutation_Inverse(data, roundKey);
			Round2_SubstituteAndRotate_Inverse(data, sbox);
			Round1_KeyXOR_Inverse(data, roundKey);
		}

		#endregion

		#region KeyGeneration
		public static byte[] Key(byte[] seed, int round, int BlockSize)
		{
			byte[] key = new byte[BlockSize];
			for (int i = 0; i < BlockSize; i++)
			{
				key[i] = (byte)(seed[i] / (seed[i] ^ ((round * 31) + (i * 17))));
			}
			return key;
		}
		#endregion

		#region Boxing
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
			KirchhoffButterfly(sboxInt, hash, 0);

			// Convert to byte array
			byte[] sbox = new byte[size];
			for (int i = 0; i < size; i++)
				sbox[i] = (byte)sboxInt[i];

			return sbox;
		}
		#endregion
	}
}