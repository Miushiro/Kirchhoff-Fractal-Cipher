using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LicenseGenerator.Encryption.KirchhoffFractals
{
	public static class Shuffles
	{
		//Thorp Shufflings
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void BaseShuffle<T>(this Random rng, T[] array)
		{
			int n = array.Length;
			while (n > 1)
			{
				int k = rng.Next(rng.Next(n--));
				(array[n], array[k]) = (array[k], array[n]);
			}
		}

		//Base Butterfly Shufffles
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ButterflyShuffle<T>(this Random rng, T[] array)
		{
			int n = array.Length;

			// Butterfly networks work cleanly only for powers of two.
			// If needed, you can pad or handle non-powers separately.
			int levels = (int)Math.Log(n, 2);
			if ((1 << levels) != n)
				throw new ArgumentException("Array length must be a power of two.");

			for (int level = 0; level < levels; level++)
			{
				int mask = 1 << level;

				for (int i = 0; i < n; i++)
				{
					int j = i ^ mask;

					// ensure each pair handled once
					if (j > i && rng.Next(2) == 1)
					{
						(array[i], array[j]) = (array[j], array[i]);
					}
				}
			}
		}

		/// <summary>
		/// Homomorphic-friendly butterfly shuffle.
		/// Uses arithmetic swap (no branching) and generates random bits from rng.
		/// Array length must be a power of 2.
		/// </summary>
		public static void HomophorbicButterflyShuffle(this Random rng, int[] array)
		{
			int n = array.Length;
			int levels = (int)Math.Log(n, 2);

			for (int level = 0; level < levels; level++)
			{
				int mask = 1 << level;

				for (int i = 0; i < n; i++)
				{
					int j = i ^ mask;

					if (j > i)
					{
						//Preserve XOR bits (0 or 1)
						int r = rng.Next(2);

						//Preserver Butterfly structures
						SimdSwap(array, i, j, r);
					}
				}
			}
		}

		// Moved SimdSwap here as a private static method
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void SimdSwap(int[] array, int i, int j, int r)
		{
			int vi = array[i];
			int vj = array[j];

			// Expand bit r to all lanes (0 → 0x00000000, 1 → 0xFFFFFFFF)
			int mask = -r; // 1 -> 0xFFFFFFFF, 0 -> 0

			// SIMD mask
			var vMask = new Vector<int>(mask);

			// Load lanes
			var a = new Vector<int>(vi);
			var b = new Vector<int>(vj);

			// Arithmetic swap via masked blend
			var newA = Vector.ConditionalSelect(vMask, b, a);
			var newB = Vector.ConditionalSelect(vMask, a, b);

			array[i] = newA[0];
			array[j] = newB[0];
		}

		//Deterministic Kirchhoff Butterfly
		public static void KirchhoffButterfly(int[] array, byte[] key, int offset)
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
	}
}
