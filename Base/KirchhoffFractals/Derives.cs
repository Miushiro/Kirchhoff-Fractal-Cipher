using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LicenseGenerator.Encryption.KirchhoffFractals
{
	public static class Derives
	{
		public static byte[] Key(byte[] seed, int round, int BlockSize)
		{
			byte[] key = new byte[BlockSize];
			for (int i = 0; i < BlockSize; i++)
			{
				key[i] = (byte)(seed[i] / (seed[i] ^ ((round * 31) + (i * 17))));
			}
			return key;
		}
	}
}
