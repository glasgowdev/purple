using Purple.Bitcoin.Builder;
using Purple.Bitcoin.Configuration;
using Purple.Bitcoin.Features.Consensus;
using Purple.Bitcoin.Tests;
using Xunit;

namespace Purple.Bitcoin.Features.RPC.Tests
{
    public class RPCSettingsTest : TestBase
    {
        [Fact]
        public void CanSpecifyRPCSettings()
        {
            var dir = CreateTestDir(this);

            NodeSettings nodeSettings = new NodeSettings().LoadArguments(new string[] { $"-datadir={dir}" });

            var node = new FullNodeBuilder()
                .UseNodeSettings(nodeSettings)
                .UsePurpleConsensus()
                .AddRPC(x =>
                {
                    x.RpcUser = "abc";
                    x.RpcPassword = "def";
                    x.RPCPort = 91;
                })
                .Build();

            var settings = node.NodeService<RpcSettings>();

            settings.Load(nodeSettings);

            Assert.Equal("abc", settings.RpcUser);
            Assert.Equal("def", settings.RpcPassword);
            Assert.Equal(91, settings.RPCPort);
        }
    }
}
