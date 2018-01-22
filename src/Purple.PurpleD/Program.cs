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
            MainAsync(args).Wait();
        }

        public static async Task MainAsync(string[] args)
        {
            try
            {
                args = new List<string>(args)
                {
                    "mine=1",
                    "mineaddress=MbPPopMepRTPQVJvohuKAjdZ66dw1ADKda"
                }.ToArray();

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
