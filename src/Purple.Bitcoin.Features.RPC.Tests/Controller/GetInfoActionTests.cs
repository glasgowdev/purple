﻿using Microsoft.Extensions.DependencyInjection;
using Purple.Bitcoin.Configuration;
using Purple.Bitcoin.Features.RPC.Controllers;
using Purple.Bitcoin.Features.RPC.Models;
using Purple.Bitcoin.Interfaces;
using Xunit;

namespace Purple.Bitcoin.Features.RPC.Tests.Controller
{
    public class GetInfoActionTests : BaseRPCControllerTest
    {
        [Fact]
        public void CallWithDependencies()
        {
            string dir = CreateTestDir(this);
            IFullNode fullNode = this.BuildStakingNode(dir);
            FullNodeController controller = fullNode.Services.ServiceProvider.GetService<FullNodeController>();

            Assert.NotNull(fullNode.NodeService<INetworkDifficulty>(true));

            GetInfoModel info = controller.GetInfo();

            NodeSettings nodeSettings = NodeSettings.Default();
            uint expectedProtocolVersion = (uint)nodeSettings.ProtocolVersion;
            var expectedRelayFee = nodeSettings.MinRelayTxFeeRate.FeePerK.ToUnit(NBitcoin.MoneyUnit.BTC);
            Assert.NotNull(info);
            Assert.Equal(0, info.Blocks);
            Assert.NotEqual<uint>(0, info.Version);
            Assert.Equal(expectedProtocolVersion, info.ProtocolVersion);
            Assert.Equal(0, info.TimeOffset);
            Assert.Equal(0, info.Connections);
            Assert.NotNull(info.Proxy);
            Assert.Equal(0, info.Difficulty);
            Assert.False(info.Testnet);
            Assert.Equal(expectedRelayFee, info.RelayFee);
            Assert.Empty(info.Errors);
        }
    }
}
