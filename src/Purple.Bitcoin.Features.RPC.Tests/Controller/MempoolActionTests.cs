﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
using Purple.Bitcoin.Features.MemoryPool;
using Xunit;

namespace Purple.Bitcoin.Features.RPC.Tests.Controller
{
    public class MempoolActionTests : BaseRPCControllerTest
    {
        [Fact]
        public async Task CanCall_GetRawMempoolAsync()
        {
            string dir = CreateTestDir(this);
            IFullNode fullNode = this.BuildStakingNode(dir);
            MempoolController controller = fullNode.Services.ServiceProvider.GetService<MempoolController>();

            List<uint256> result = await controller.GetRawMempool();

            Assert.NotNull(result);
        }
    }
}
