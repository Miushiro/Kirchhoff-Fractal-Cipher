using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace LicenseGenerator.Encryption.KirchhoffFractals.Forward
{
	public static class SIMDShuffles
	{
		public static void SIMDForwardShuffles(byte[] data, byte[] roundKey, byte[] sbox)
		{
			StrongRoundHomomorphic(data, roundKey, sbox);
		}

		private static void StrongRoundHomomorphic(byte[] data, byte[] roundKey, byte[] sbox)
		{
			int n = data.Length;
			int vectorSize = Vector<byte>.Count;

			// --- SIMD XOR (FIRST) ---
			// FIX: Properly handle key wrapping with extended key buffer
			byte[] extendedKey = new byte[Math.Max(roundKey.Length, ((n + vectorSize - 1) / vectorSize) * vectorSize)];
			for (int i = 0; i < extendedKey.Length; i++)
				extendedKey[i] = roundKey[i % roundKey.Length];


			for (int i = 0; i < n; i += vectorSize)
			{
				if (i + vectorSize <= n)
				{
					var block = new Vector<byte>(data, i);
					var keyBlock = new Vector<byte>(extendedKey, i % roundKey.Length);
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

			// --- SBox and rotation: 16-byte blocks ---
			for (int i = 0; i < n; i += 16)
			{
				for (int j = 0; j < 16 && (i + j) < n; j++)
				{
					data[i + j] = sbox[data[i + j]];
					data[i + j] = (byte)((data[i + j] << 3) | (data[i + j] >> 5));
				}
			}

			// --- Homomorphic butterfly shuffle ---
			// Convert to int[] for homomorphic arithmetic swap
			int[] dataInt = Array.ConvertAll(data, b => (int)b);

			// Deterministic RNG from roundKey
			int seed = 0;
			for (int i = 0; i < 4 && i < roundKey.Length; i++) // combine first 4 bytes into seed
				seed = (seed << 8) | roundKey[i];

			// Shuffle using your homomorphic butterfly
			Random rng = new Random(seed);
			rng.HomophorbicButterflyShuffle(dataInt);

			// Convert back to byte[]
			for (int i = 0; i < n; i++) data[i] = (byte)dataInt[i];
		}

	}
}
