using System;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.Protocol;
using Purple.Bitcoin;
using Purple.Bitcoin.Builder;
using Purple.Bitcoin.Configuration;
using Purple.Bitcoin.Features.Api;
using Purple.Bitcoin.Features.LightWallet;
using Purple.Bitcoin.Features.Notifications;
using Purple.Bitcoin.Utilities;

namespace Purple.BreezeD
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
                // Get the API uri.
                var isTestNet = args.Contains("-testnet");
                var isPurple = args.Contains("Purple");

                var agent = "Breeze";

                NodeSettings nodeSettings;

                if (isPurple)
                {
                    if (NodeSettings.PrintHelp(args, Network.PurpleMain))
                        return;

                    Network network = isTestNet ? Network.PurpleTest : Network.PurpleMain;
                    if (isTestNet)
                        args = args.Append("-addnode=51.141.28.47").ToArray(); // TODO: fix this temp hack

                    nodeSettings = new NodeSettings("Purple", network, ProtocolVersion.ALT_PROTOCOL_VERSION, agent).LoadArguments(args);
                }
                else
                {
                    nodeSettings = new NodeSettings(agent: agent).LoadArguments(args);
                }

                IFullNodeBuilder fullNodeBuilder = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings)
                    .UseLightWallet()
                    .UseBlockNotification()
                    .UseTransactionNotification()
                    .UseApi();

                IFullNode node = fullNodeBuilder.Build();

                // Start Full Node - this will also start the API.
                await node.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem initializing the node. Details: '{0}'", ex.Message);
            }
        }
    }
}
