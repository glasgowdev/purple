using System;
using System.Threading.Tasks;
using Purple.Bitcoin.Builder;
using Purple.Bitcoin.Configuration;
using Purple.Bitcoin.Features.BlockStore;
using Purple.Bitcoin.Features.Consensus;
using Purple.Bitcoin.Features.MemoryPool;
using Purple.Bitcoin.Features.Miner;
using Purple.Bitcoin.Features.RPC;
using Purple.Bitcoin.Features.Wallet;
using Purple.Bitcoin.Utilities;

namespace Purple.BitcoinD
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
                NodeSettings nodeSettings = new NodeSettings().LoadArguments(args);

                var node = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings)
                    .UseConsensus()
                    .UseBlockStore()
                    .UseMempool()
                    .AddMining()
                    .AddRPC()
                    .UseWallet()
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
