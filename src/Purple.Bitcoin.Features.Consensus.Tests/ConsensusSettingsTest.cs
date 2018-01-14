using Microsoft.Extensions.Logging;
using NBitcoin;
using Purple.Bitcoin.Configuration;
using Xunit;

namespace Purple.Bitcoin.Features.Consensus.Tests
{
    public class ConsensusSettingsTest
    {
        [Fact]
        public void LoadConfigWithAssumeValidHexLoads()
        {
            uint256 validHexBlock = new uint256("00000000229d9fb87182d73870d53f9fdd9b76bfc02c059e6d9a6c7a3507031d");
            LoggerFactory loggerFactory = new LoggerFactory();
            Network network = Network.PurpleTest;
            NodeSettings nodeSettings = new NodeSettings(network.Name, network).LoadArguments(new string[] { $"-assumevalid={validHexBlock.ToString()}" });
            ConsensusSettings settings = new ConsensusSettings(nodeSettings, loggerFactory);
            Assert.Equal(validHexBlock, settings.BlockAssumedValid);
        }

        [Fact]
        public void LoadConfigWithAssumeValidZeroSetsToNull()
        {
            LoggerFactory loggerFactory = new LoggerFactory();
            Network network = Network.PurpleTest;
            NodeSettings nodeSettings = new NodeSettings(network.Name, network).LoadArguments(new string[] { "-assumevalid=0" });
            ConsensusSettings settings = new ConsensusSettings(nodeSettings, loggerFactory);
            Assert.Null(settings.BlockAssumedValid);
        }

        [Fact]
        public void LoadConfigWithInvalidAssumeValidThrowsConfigException()
        {
            LoggerFactory loggerFactory = new LoggerFactory();
            Network network = Network.PurpleTest;
            NodeSettings nodeSettings = new NodeSettings(network.Name, network).LoadArguments(new string[] { "-assumevalid=xxx" });
            Assert.Throws<ConfigurationException>(() => new ConsensusSettings(nodeSettings, loggerFactory));
        }

        [Fact]
        public void LoadConfigWithDefaultsSetsToNetworkDefault()
        {
            LoggerFactory loggerFactory = new LoggerFactory();

            Network network = Network.PurpleMain;
            ConsensusSettings settings = new ConsensusSettings(NodeSettings.Default(network), loggerFactory);
            Assert.Equal(network.Consensus.DefaultAssumeValid, settings.BlockAssumedValid);

            network = Network.PurpleTest;
            settings = new ConsensusSettings(NodeSettings.Default(network), loggerFactory);
            Assert.Equal(network.Consensus.DefaultAssumeValid, settings.BlockAssumedValid);

            network = Network.PurpleMain;
            settings = new ConsensusSettings(NodeSettings.Default(network), loggerFactory);
            Assert.Equal(network.Consensus.DefaultAssumeValid, settings.BlockAssumedValid);

            network = Network.PurpleTest;
            settings = new ConsensusSettings(NodeSettings.Default(network), loggerFactory);
            Assert.Equal(network.Consensus.DefaultAssumeValid, settings.BlockAssumedValid);
        }
    }
}
