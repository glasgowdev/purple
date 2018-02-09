using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace NBitcoin.Crypto
{
    public sealed class CryptoNight
    {
        [DllImport(@"Purple.CryptoNight_x64.dll", EntryPoint = "cn_slow_hash_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern void hardware_hash_x64(byte[] input, byte[] output, uint inputLength);

        [DllImport(@"Purple.CryptoNight_x86.dll", EntryPoint = "cn_slow_hash_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern void hardware_hash_x86(byte[] input, byte[] output, uint inputLength);


        private static readonly Lazy<CryptoNight> SingletonInstance = new Lazy<CryptoNight>(LazyThreadSafetyMode.PublicationOnly);

        public static CryptoNight Instance => SingletonInstance.Value;

        public static CryptoNight Create()
        {
            return new CryptoNight();
        }

        public uint256 Hash(byte[] input)
        {
            byte[] buffer = new byte[32];

            if (Environment.Is64BitProcess)
            {
                hardware_hash_x64(input, buffer, (uint)input.Length);
            }
            else
            {
                hardware_hash_x86(input, buffer, (uint)input.Length);
            }

            return new uint256(buffer.Take(32).ToArray());
        }
    }
}
