using System;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.Protocol;
using Purple.Bitcoin;
using Purple.Bitcoin.Builder;
using Purple.Bitcoin.Configuration;
using Purple.Bitcoin.Features.Api;
using Purple.Bitcoin.Features.BlockStore;
using Purple.Bitcoin.Features.Consensus;
using Purple.Bitcoin.Features.Dns;
using Purple.Bitcoin.Features.MemoryPool;
using Purple.Bitcoin.Features.Miner;
using Purple.Bitcoin.Features.RPC;
using Purple.Bitcoin.Features.Wallet;
using Purple.Bitcoin.Utilities;

namespace Purple.PurpleDnsD
{
    /// <summary>
    /// Main entry point.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The entry point for the Purple Dns process.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        /// <summary>
        /// The async entry point for the Purple Dns process.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>A task used to await the operation.</returns>
        public static async Task MainAsync(string[] args)
        {
            try
            {
                Network network = args.Contains("-testnet") ? Network.PurpleTest : Network.PurpleMain;
                NodeSettings nodeSettings = new NodeSettings("Purple", network, ProtocolVersion.ALT_PROTOCOL_VERSION).LoadArguments(args);
                DnsSettings dnsSettings = DnsSettings.Load(nodeSettings);

                // Verify that the DNS host, nameserver and mailbox arguments are set.
                if (string.IsNullOrWhiteSpace(dnsSettings.DnsHostName) || string.IsNullOrWhiteSpace(dnsSettings.DnsNameServer) || string.IsNullOrWhiteSpace(dnsSettings.DnsMailBox))
                {
                    throw new ArgumentException("When running as a DNS Seed service, the -dnshostname, -dnsnameserver and -dnsmailbox arguments must be specified on the command line.");
                }

                // Run as a full node with DNS or just a DNS service?
                if (dnsSettings.DnsFullNode)
                {
                    // Build the Dns full node.
                    IFullNode node = new FullNodeBuilder()
                        .UseNodeSettings(nodeSettings)
                        .UsePurpleConsensus()
                        .UseBlockStore()
                        .UseMempool()
                        .UseWallet()
                        .AddPowPosMining()
                        .UseApi()
                        .AddRPC()
                        .UseDns()
                        .Build();

                    // Run node.
                    await node.RunAsync();
                }
                else
                {
                    // Build the Dns node.
                    IFullNode node = new FullNodeBuilder()
                        .UseNodeSettings(nodeSettings)
                        .UsePurpleConsensus()
                        .UseApi()
                        .AddRPC()
                        .UseDns()
                        .Build();

                    // Run node.
                    await node.RunAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem initializing the node. Details: '{0}'", ex.Message);
            }
        }
    }
}
