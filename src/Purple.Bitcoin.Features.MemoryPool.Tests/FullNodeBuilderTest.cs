using System;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
using Purple.Bitcoin.Base;
using Purple.Bitcoin.Builder;
using Purple.Bitcoin.Configuration;
using Purple.Bitcoin.Connection;
using Purple.Bitcoin.Features.BlockStore;
using Purple.Bitcoin.Features.Consensus;
using Xunit;

namespace Purple.Bitcoin.Features.MemoryPool.Tests
{
    public class FullNodeBuilderTest
    {
        [Fact]
        public void CanHaveAllFullnodeServicesTest()
        {
            // This test is put in the mempool feature because the 
            // mempool requires all the features to be a fullnode


            var nodeSettings = NodeSettings.Default();
            nodeSettings.DataDir = "Purple.Bitcoin.Features.MemoryPool.Tests/TestData/FullNodeBuilderTest/CanHaveAllServicesTest";
            var fullNodeBuilder = new FullNodeBuilder(nodeSettings);
            IFullNode fullNode = fullNodeBuilder
                .UsePurpleConsensus()
                .UseBlockStore()
                .UseMempool()
                .Build();

            IServiceProvider serviceProvider = fullNode.Services.ServiceProvider;
            var network = serviceProvider.GetService<Network>();
            var settings = serviceProvider.GetService<NodeSettings>();
            var consensusLoop = serviceProvider.GetService<ConsensusLoop>();
            var consensus = serviceProvider.GetService<PosConsensusValidator>();
            var chain = serviceProvider.GetService<NBitcoin.ConcurrentChain>();
            var chainState = serviceProvider.GetService<ChainState>();
            var blockStoreManager = serviceProvider.GetService<BlockStoreManager>();
            var mempoolManager = serviceProvider.GetService<MempoolManager>();
            var connectionManager = serviceProvider.GetService<IConnectionManager>();

            Assert.NotNull(fullNode);
            Assert.NotNull(network);
            Assert.NotNull(settings);
            Assert.NotNull(consensusLoop);
            Assert.NotNull(consensus);
            Assert.NotNull(chain);
            Assert.NotNull(chainState);
            Assert.NotNull(blockStoreManager);
            Assert.NotNull(mempoolManager);
            Assert.NotNull(connectionManager);
        }
    }
}
