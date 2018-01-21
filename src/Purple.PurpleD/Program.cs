using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using Purple.Bitcoin.Builder;
using Purple.Bitcoin.Configuration;
using Purple.Bitcoin.Features.Api;
using Purple.Bitcoin.Features.BlockStore;
using Purple.Bitcoin.Features.Consensus;
using Purple.Bitcoin.Features.MemoryPool;
using Purple.Bitcoin.Features.Miner;
using Purple.Bitcoin.Features.RPC;
using Purple.Bitcoin.Features.Wallet;
using Purple.Bitcoin.Utilities;

namespace Purple.PurpleD
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //ParallelHash();
            MainAsync(args).Wait();
        }

        public static void ParallelHash()
        {
            int nonce = 0;
            int count = 0;

            var watch = new Stopwatch();
            watch.Start();

            var target = new uint256("0xeb14e8a833fac6fe9a43b57b336789c46ffe93f2868452240720607b14387e11");

            //Parallel.ForEach(Infinite(), (x, state) =>
            //{
            //    if (!state.ShouldExitCurrentIteration)
            //    {
            //        var hash = NBitcoin.Crypto.CryptoNight.Instance.Hash(Encoding.UTF8.GetBytes(""));
            //        var match = hash == target;
            //        Interlocked.Increment(ref nonce);
            //        if (nonce == 1000)
            //        {
            //            state.Break();
            //        }
            //    }
            //});

            //while (nonce < 1000)
            //{


            //    ++nonce;
            //    if (watch.ElapsedMilliseconds > 1000)
            //    {
            //        Console.WriteLine($"{nonce - last} H/s");
            //        last = nonce;
            //        watch.Restart();
            //    }
            //}

            watch.Stop();

            Console.WriteLine(watch.Elapsed.TotalSeconds);
            Console.ReadLine();
        }



        public static async Task MainAsync(string[] args)
        {
            try
            {
                // Network network = args.Contains("-testnet") ? Network.PurpleTest : Network.PurpleMain;
                Network network = Network.PurpleRegTest;
                NodeSettings nodeSettings = new NodeSettings("Purple", network, ProtocolVersion.ALT_PROTOCOL_VERSION).LoadArguments(args);

                var node = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings)
                    .UsePurpleConsensus()
                    .UseBlockStore()
                    .UseMempool()
                    .UseWallet()
                    .AddPowPosMining()
                    .UseApi()
                    .AddRPC()
                    .Build();

                await node.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem initializing the node. Details: '{0}'", ex.Message);
            }
        }
    }
}
