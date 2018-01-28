using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace NBitcoin.Crypto
{
    public sealed class CryptoNight
    {
        [DllImport(@"CryptoNight.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void hardware_hash(byte[] input, int size, byte[] output);

        [DllImport(@"CryptoNight.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void software_hash(byte[] input, int size, byte[] output);

        private static readonly Lazy<CryptoNight> SingletonInstance = new Lazy<CryptoNight>(LazyThreadSafetyMode.PublicationOnly);

        public static CryptoNight Instance => SingletonInstance.Value;

        public static CryptoNight Create()
        {
            return new CryptoNight();
        }

        public uint256 Hash(byte[] input, bool hardware = true)
        {
            byte[] buffer = new byte[32];

            if (hardware)
            {
                hardware_hash(input, input.Length, buffer);
            }
            else
            {
                software_hash(input, input.Length, buffer);
            }

            return new uint256(buffer.Take(32).ToArray());
        }
    }
}
