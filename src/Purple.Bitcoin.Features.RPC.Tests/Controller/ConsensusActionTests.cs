using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
using Purple.Bitcoin.Features.Consensus;
using Purple.Bitcoin.Interfaces;
using Xunit;

namespace Purple.Bitcoin.Features.RPC.Tests.Controller
{
    public class ConsensusActionTests : BaseRPCControllerTest
    {
        public ConsensusActionTests()
        {
        }

        [Fact]
        public void CanCall_GetBestBlockHash()
        {
            string dir = CreateTestDir(this);

            var fullNode = this.BuildStakingNode(dir);
            var controller = fullNode.Services.ServiceProvider.GetService<ConsensusController>();

            uint256 result = controller.GetBestBlockHash();

            Assert.Null(result);
        }

        [Fact]
        public void CanCall_GetBlockHash()
        {
            string dir = CreateTestDir(this);

            var fullNode = this.BuildStakingNode(dir);
            var controller = fullNode.Services.ServiceProvider.GetService<ConsensusController>();

            uint256 result = controller.GetBlockHash(0);

            Assert.Null(result);
        }

        [Fact]
        public void CanCall_IsInitialBlockDownload()
        {
            string dir = CreateTestDir(this);

            var fullNode = this.BuildStakingNode(dir);
            var isIBDProvider = fullNode.NodeService<IInitialBlockDownloadState>(true);

            Assert.NotNull(isIBDProvider);
            Assert.True(isIBDProvider.IsInitialBlockDownload());
        }
    }
}
