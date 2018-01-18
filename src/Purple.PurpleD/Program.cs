using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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
                Network network = args.Contains("-testnet") ? Network.PurpleTest : Network.PurpleMain;
                NodeSettings nodeSettings = new NodeSettings("Purple", network, ProtocolVersion.ALT_PROTOCOL_VERSION).LoadArguments(args);

                // NOTES: running BTC and PPL side by side is not possible yet as the flags for serialization are static

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
