using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace LicenseGenerator.Encryption.KirchhoffFractals.Inverse
{
	public static class SIMDShuffles
	{
		public static void SIMDInverseShuffles(byte[] data, byte[] roundKey, byte[] sbox)
		{
			StrongRoundInverseHomomorphic(data, roundKey, sbox);
		}
		private static void StrongRoundInverseHomomorphic(byte[] data, byte[] roundKey, byte[] sbox)
		{
			int n = data.Length;

			// --- Convert data to int[] for homomorphic shuffle ---
			int[] dataInt = Array.ConvertAll(data, b => (int)b);

			// --- Deterministic RNG from roundKey (must match forward) ---
			int seed = 0;
			for (int i = 0; i < 4 && i < roundKey.Length; i++)
				seed = (seed << 8) | roundKey[i];

			Random rng = new Random(seed);
			rng.HomophorbicButterflyShuffle(dataInt);

			// --- Convert back to byte[] ---
			for (int i = 0; i < n; i++)
				data[i] = (byte)dataInt[i];

			// --- Reverse rotation and inverse SBox ---
			byte[] inverseSBox = new byte[256];
			for (int i = 0; i < 256; i++)
				inverseSBox[sbox[i]] = (byte)i;

			for (int i = 0; i < n; i += 16)
			{
				for (int j = 0; j < 16 && (i + j) < n; j++)
				{
					data[i + j] = (byte)((data[i + j] >> 3) | (data[i + j] << 5));
					data[i + j] = inverseSBox[data[i + j]];
				}
			}

			// --- SIMD XOR with round key ---
			int vectorSize = Vector<byte>.Count;

			// Properly handle key wrapping with extended key buffer
			byte[] extendedKey = new byte[Math.Max(roundKey.Length, ((n + vectorSize - 1) / vectorSize) * vectorSize)];
			for (int i = 0; i < extendedKey.Length; i++)
				extendedKey[i] = roundKey[i % roundKey.Length];

			// --- SIMD XOR with round key (LAST - reverse of forward) ---
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
		}

	}
}
