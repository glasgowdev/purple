using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Purple.Bitcoin.Connection;
using Purple.Bitcoin.IntegrationTests.EnvironmentMockUpHelpers;
using Xunit;

namespace Purple.Bitcoin.IntegrationTests
{
    public class NodeSyncTests
    {
        public NodeSyncTests()
        {
            // These tests are for mostly for POW. Set the flags to the expected values.
            Transaction.TimeStamp = false;
            Block.BlockSignature = false;
        }

        [Fact]
        public void NodesCanConnectToEachOthers()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var node1 = builder.CreatePurplePowNode();
                var node2 = builder.CreatePurplePowNode();
                builder.StartAll();
                Assert.Empty(node1.FullNode.ConnectionManager.ConnectedPeers);
                Assert.Empty(node2.FullNode.ConnectionManager.ConnectedPeers);
                var rpc1 = node1.CreateRPCClient();
                rpc1.AddNode(node2.Endpoint, true);
                Assert.Single(node1.FullNode.ConnectionManager.ConnectedPeers);
                Assert.Single(node2.FullNode.ConnectionManager.ConnectedPeers);

                var behavior = node1.FullNode.ConnectionManager.ConnectedPeers.First().Behaviors.Find<ConnectionManagerBehavior>();
                Assert.False(behavior.Inbound);
                Assert.True(behavior.OneTry);
                behavior = node2.FullNode.ConnectionManager.ConnectedPeers.First().Behaviors.Find<ConnectionManagerBehavior>();
                Assert.True(behavior.Inbound);
                Assert.False(behavior.OneTry);
            }
        }

        [Fact]
        public void CanPurpleSyncFromCore()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var PurpleNode = builder.CreatePurplePowNode();
                var coreNode = builder.CreateNode();
                builder.StartAll();

                PurpleNode.NotInIBD();

                var tip = coreNode.FindBlock(10).Last();
                PurpleNode.CreateRPCClient().AddNode(coreNode.Endpoint, true);
                TestHelper.WaitLoop(() => PurpleNode.CreateRPCClient().GetBestBlockHash() == coreNode.CreateRPCClient().GetBestBlockHash());
                var bestBlockHash = PurpleNode.CreateRPCClient().GetBestBlockHash();
                Assert.Equal(tip.GetHash(), bestBlockHash);

                //Now check if Core connect to Purple
                PurpleNode.CreateRPCClient().RemoveNode(coreNode.Endpoint);
                tip = coreNode.FindBlock(10).Last();
                coreNode.CreateRPCClient().AddNode(PurpleNode.Endpoint, true);
                TestHelper.WaitLoop(() => PurpleNode.CreateRPCClient().GetBestBlockHash() == coreNode.CreateRPCClient().GetBestBlockHash());
                bestBlockHash = PurpleNode.CreateRPCClient().GetBestBlockHash();
                Assert.Equal(tip.GetHash(), bestBlockHash);
            }
        }

        [Fact]
        public void CanPurpleSyncFromPurple()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var PurpleNode = builder.CreatePurplePowNode();
                var PurpleNodeSync = builder.CreatePurplePowNode();
                var coreCreateNode = builder.CreateNode();
                builder.StartAll();

                PurpleNode.NotInIBD();
                PurpleNodeSync.NotInIBD();

                // first seed a core node with blocks and sync them to a Purple node
                // and wait till the Purple node is fully synced
                var tip = coreCreateNode.FindBlock(5).Last();
                PurpleNode.CreateRPCClient().AddNode(coreCreateNode.Endpoint, true);
                TestHelper.WaitLoop(() => PurpleNode.CreateRPCClient().GetBestBlockHash() == coreCreateNode.CreateRPCClient().GetBestBlockHash());
                var bestBlockHash = PurpleNode.CreateRPCClient().GetBestBlockHash();
                Assert.Equal(tip.GetHash(), bestBlockHash);

                // add a new Purple node which will download
                // the blocks using the GetData payload
                PurpleNodeSync.CreateRPCClient().AddNode(PurpleNode.Endpoint, true);

                // wait for download and assert
                TestHelper.WaitLoop(() => PurpleNode.CreateRPCClient().GetBestBlockHash() == PurpleNodeSync.CreateRPCClient().GetBestBlockHash());
                bestBlockHash = PurpleNodeSync.CreateRPCClient().GetBestBlockHash();
                Assert.Equal(tip.GetHash(), bestBlockHash);
            }
        }

        [Fact]
        public void CanCoreSyncFromPurple()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var PurpleNode = builder.CreatePurplePowNode();
                var coreNodeSync = builder.CreateNode();
                var coreCreateNode = builder.CreateNode();
                builder.StartAll();

                PurpleNode.NotInIBD();

                // first seed a core node with blocks and sync them to a Purple node
                // and wait till the Purple node is fully synced
                var tip = coreCreateNode.FindBlock(5).Last();
                PurpleNode.CreateRPCClient().AddNode(coreCreateNode.Endpoint, true);
                TestHelper.WaitLoop(() => PurpleNode.CreateRPCClient().GetBestBlockHash() == coreCreateNode.CreateRPCClient().GetBestBlockHash());
                TestHelper.WaitLoop(() => PurpleNode.FullNode.HighestPersistedBlock().HashBlock == PurpleNode.FullNode.Chain.Tip.HashBlock);

                var bestBlockHash = PurpleNode.CreateRPCClient().GetBestBlockHash();
                Assert.Equal(tip.GetHash(), bestBlockHash);

                // add a new Purple node which will download
                // the blocks using the GetData payload
                coreNodeSync.CreateRPCClient().AddNode(PurpleNode.Endpoint, true);

                // wait for download and assert
                TestHelper.WaitLoop(() => PurpleNode.CreateRPCClient().GetBestBlockHash() == coreNodeSync.CreateRPCClient().GetBestBlockHash());
                bestBlockHash = coreNodeSync.CreateRPCClient().GetBestBlockHash();
                Assert.Equal(tip.GetHash(), bestBlockHash);
            }
        }

        [Fact]
        public void Given__NodesAreSynced__When__ABigReorgHappens__Then__TheReorgIsIgnored()
        {
            // Temporary fix so the Network static initialize will not break.
            var m = Network.Main;
            Transaction.TimeStamp = true;
            Block.BlockSignature = true;
            try
            {
                using (NodeBuilder builder = NodeBuilder.Create())
                {
                    var PurpleMiner = builder.CreatePurplePosNode();
                    var PurpleSyncer = builder.CreatePurplePosNode();
                    var PurpleReorg = builder.CreatePurplePosNode();

                    builder.StartAll();
                    PurpleMiner.NotInIBD();
                    PurpleSyncer.NotInIBD();
                    PurpleReorg.NotInIBD();

                    // TODO: set the max allowed reorg threshold here
                    // assume a reorg of 10 blocks is not allowed.
                    PurpleMiner.FullNode.ChainBehaviorState.MaxReorgLength = 10;
                    PurpleSyncer.FullNode.ChainBehaviorState.MaxReorgLength = 10;
                    PurpleReorg.FullNode.ChainBehaviorState.MaxReorgLength = 10;

                    PurpleMiner.SetDummyMinerSecret(new BitcoinSecret(new Key(), PurpleMiner.FullNode.Network));
                    PurpleReorg.SetDummyMinerSecret(new BitcoinSecret(new Key(), PurpleReorg.FullNode.Network));

                    PurpleMiner.GeneratePurpleWithMiner(1);

                    // wait for block repo for block sync to work
                    TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(PurpleMiner));
                    PurpleMiner.CreateRPCClient().AddNode(PurpleReorg.Endpoint, true);
                    PurpleMiner.CreateRPCClient().AddNode(PurpleSyncer.Endpoint, true);

                    TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleMiner, PurpleSyncer));
                    TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleMiner, PurpleReorg));

                    // create a reorg by mining on two different chains
                    // ================================================

                    PurpleMiner.CreateRPCClient().RemoveNode(PurpleReorg.Endpoint);
                    PurpleSyncer.CreateRPCClient().RemoveNode(PurpleReorg.Endpoint);

                    var t1 = Task.Run(() => PurpleMiner.GeneratePurpleWithMiner(11));
                    var t2 = Task.Delay(1000).ContinueWith(t => PurpleReorg.GeneratePurpleWithMiner(12));
                    Task.WaitAll(t1, t2);
                    TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(PurpleMiner));
                    TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(PurpleReorg));

                    // make sure the nodes are actually on different chains.
                    Assert.NotEqual(PurpleMiner.FullNode.Chain.GetBlock(2).HashBlock, PurpleReorg.FullNode.Chain.GetBlock(2).HashBlock);

                    TestHelper.TriggerSync(PurpleSyncer);
                    TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleMiner, PurpleSyncer));

                    // The hash before the reorg node is connected.
                    var hashBeforeReorg = PurpleMiner.FullNode.Chain.Tip.HashBlock;

                    // connect the reorg chain
                    PurpleMiner.CreateRPCClient().AddNode(PurpleReorg.Endpoint, true);
                    PurpleSyncer.CreateRPCClient().AddNode(PurpleReorg.Endpoint, true);

                    // trigger nodes to sync
                    TestHelper.TriggerSync(PurpleMiner);
                    TestHelper.TriggerSync(PurpleReorg);
                    TestHelper.TriggerSync(PurpleSyncer);

                    // wait for the synced chain to get headers updated.
                    TestHelper.WaitLoop(() => !PurpleReorg.FullNode.ConnectionManager.ConnectedPeers.Any());

                    TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleMiner, PurpleSyncer));
                    TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleReorg, PurpleMiner) == false);
                    TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleReorg, PurpleSyncer) == false);

                    // check that a reorg did not happen.
                    Assert.Equal(hashBeforeReorg, PurpleSyncer.FullNode.Chain.Tip.HashBlock);
                }
            }
            finally
            {
                Transaction.TimeStamp = false;
                Block.BlockSignature = false;
            }
        }

        /// <summary>
        /// This tests simulates scenario 2 from issue 636.
        /// <para>
        /// The test mines a block and roughly at the same time, but just after that, a new block at the same height
        /// arrives from the puller. Then another block comes from the puller extending the chain without the block we mined.
        /// </para>
        /// </summary>
        /// <seealso cref="https://github.com/Stratisproject/StratisBitcoinFullNode/issues/636"/>
        [Fact]
        public void PullerVsMinerRaceCondition()
        {
            // Temporary fix so the Network static initialize will not break.
            var m = Network.Main;
            Transaction.TimeStamp = true;
            Block.BlockSignature = true;
            try
            {
                using (NodeBuilder builder = NodeBuilder.Create())
                {
                    // This represents local node.
                    var PurpleMinerLocal = builder.CreatePurplePosNode();

                    // This represents remote, which blocks are received by local node using its puller.
                    var PurpleMinerRemote = builder.CreatePurplePosNode();

                    builder.StartAll();
                    PurpleMinerLocal.NotInIBD();
                    PurpleMinerRemote.NotInIBD();

                    PurpleMinerLocal.SetDummyMinerSecret(new BitcoinSecret(new Key(), PurpleMinerLocal.FullNode.Network));
                    PurpleMinerRemote.SetDummyMinerSecret(new BitcoinSecret(new Key(), PurpleMinerRemote.FullNode.Network));

                    // Let's mine block Ap and Bp.
                    PurpleMinerRemote.GeneratePurpleWithMiner(2);

                    // Wait for block repository for block sync to work.
                    TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(PurpleMinerRemote));
                    PurpleMinerLocal.CreateRPCClient().AddNode(PurpleMinerRemote.Endpoint, true);

                    TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleMinerLocal, PurpleMinerRemote));

                    // Now disconnect the peers and mine block C2p on remote.
                    PurpleMinerLocal.CreateRPCClient().RemoveNode(PurpleMinerRemote.Endpoint);

                    // Mine block C2p.
                    PurpleMinerRemote.GeneratePurpleWithMiner(1);
                    Thread.Sleep(2000);

                    // Now reconnect nodes and mine block C1s before C2p arrives.
                    PurpleMinerLocal.CreateRPCClient().AddNode(PurpleMinerRemote.Endpoint, true);
                    PurpleMinerLocal.GeneratePurpleWithMiner(1);

                    // Mine block Dp.
                    uint256 dpHash = PurpleMinerRemote.GeneratePurpleWithMiner(1)[0];

                    // Now we wait until the local node's chain tip has correct hash of Dp.
                    TestHelper.WaitLoop(() => PurpleMinerLocal.FullNode.Chain.Tip.HashBlock.Equals(dpHash));

                    // Then give it time to receive the block from the puller.
                    Thread.Sleep(2500);

                    // Check that local node accepted the Dp as consensus tip.
                    Assert.Equal(PurpleMinerLocal.FullNode.ChainBehaviorState.ConsensusTip.HashBlock, dpHash);
                }
            }
            finally
            {
                Transaction.TimeStamp = false;
                Block.BlockSignature = false;
            }
        }

        /// <summary>
        /// This test simulates scenario from issue #862.
        /// <para>
        /// Connection scheme:
        /// Network - Node1 - MiningNode
        /// </para>
        /// </summary>
        [Fact]
        public void MiningNodeWithOneConnectionAlwaysSynced()
        {
            NetworkSimulator simulator = new NetworkSimulator();

            simulator.Initialize(4);

            var miner = simulator.Nodes[0];
            var connector = simulator.Nodes[1];
            var networkNode1 = simulator.Nodes[2];
            var networkNode2 = simulator.Nodes[3];

            // Connect nodes with each other. Miner is connected to connector and connector, node1, node2 are connected with each other.
            miner.CreateRPCClient().AddNode(connector.Endpoint, true);
            connector.CreateRPCClient().AddNode(networkNode1.Endpoint, true);
            connector.CreateRPCClient().AddNode(networkNode2.Endpoint, true);
            networkNode1.CreateRPCClient().AddNode(networkNode2.Endpoint, true);

            simulator.MakeSureEachNodeCanMineAndSync();

            int networkHeight = miner.FullNode.Chain.Height;
            Assert.Equal(networkHeight, simulator.Nodes.Count);

            // Random node on network generates a block.
            networkNode1.GeneratePurple(1);

            // Wait until connector get the hash of network's block.
            while ((connector.FullNode.ChainBehaviorState.ConsensusTip.HashBlock != networkNode1.FullNode.ChainBehaviorState.ConsensusTip.HashBlock) ||
                   (networkNode1.FullNode.ChainBehaviorState.ConsensusTip.Height == networkHeight))
                Thread.Sleep(1);

            // Make sure that miner did not advance yet but connector did.
            Assert.NotEqual(miner.FullNode.Chain.Tip.HashBlock, networkNode1.FullNode.Chain.Tip.HashBlock);
            Assert.Equal(connector.FullNode.Chain.Tip.HashBlock, networkNode1.FullNode.Chain.Tip.HashBlock);
            Assert.Equal(miner.FullNode.Chain.Tip.Height, networkHeight);
            Assert.Equal(connector.FullNode.Chain.Tip.Height, networkHeight+1);

            // Miner mines the block.
            miner.GeneratePurple(1);
            TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(miner));

            networkHeight++;

            // Make sure that at this moment miner's tip != network's and connector's tip.
            Assert.NotEqual(miner.FullNode.Chain.Tip.HashBlock, networkNode1.FullNode.Chain.Tip.HashBlock);
            Assert.Equal(connector.FullNode.Chain.Tip.HashBlock, networkNode1.FullNode.Chain.Tip.HashBlock);
            Assert.Equal(miner.FullNode.Chain.Tip.Height, networkHeight);
            Assert.Equal(connector.FullNode.Chain.Tip.Height, networkHeight);

            connector.GeneratePurple(1);
            networkHeight++;

            int delay = 0;

            while (true)
            {
                Thread.Sleep(50);
                if (simulator.DidAllNodesReachHeight(networkHeight))
                    break;
                delay += 50;

                Assert.True(delay < 10 * 1000, "Miner node was not able to advance!");
            }

            Assert.Equal(networkNode1.FullNode.Chain.Tip.HashBlock, miner.FullNode.Chain.Tip.HashBlock);
        }
    }
}
