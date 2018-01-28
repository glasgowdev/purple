using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace NBitcoin.Crypto
{
    public sealed class CryptoNight
    {
        [DllImport(@"Purple.CryptoNight_x64.dll", EntryPoint = "hardware_hash", CallingConvention = CallingConvention.Cdecl)]
        public static extern void hardware_hash_x64(byte[] input, int size, byte[] output);

        [DllImport(@"Purple.CryptoNight_x64.dll", EntryPoint = "software_hash", CallingConvention = CallingConvention.Cdecl)]
        public static extern void software_hash_x64(byte[] input, int size, byte[] output);

        [DllImport(@"Purple.CryptoNight_x86.dll", EntryPoint = "hardware_hash", CallingConvention = CallingConvention.Cdecl)]
        public static extern void hardware_hash_x86(byte[] input, int size, byte[] output);

        [DllImport(@"Purple.CryptoNight_x86.dll", EntryPoint = "software_hash", CallingConvention = CallingConvention.Cdecl)]
        public static extern void software_hash_x86(byte[] input, int size, byte[] output);

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
                if (Environment.Is64BitProcess)
                {
                    hardware_hash_x64(input, input.Length, buffer);
                }
                else
                {
                    hardware_hash_x86(input, input.Length, buffer);
                }
            }
            else
            {
                if (Environment.Is64BitProcess)
                {
                    software_hash_x64(input, input.Length, buffer);
                }
                else
                {
                    software_hash_x86(input, input.Length, buffer);
                }
            }

            return new uint256(buffer.Take(32).ToArray());
        }
    }
}
