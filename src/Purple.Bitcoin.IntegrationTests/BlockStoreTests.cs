using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Purple.Bitcoin.Connection;
using Purple.Bitcoin.Features.BlockStore;
using Purple.Bitcoin.IntegrationTests.EnvironmentMockUpHelpers;
using Purple.Bitcoin.Utilities;
using Xunit;

namespace Purple.Bitcoin.IntegrationTests
{
    public class BlockStoreTests
    {
        /// <summary>Factory for creating loggers.</summary>
        protected readonly ILoggerFactory loggerFactory;

        /// <summary>
        /// Initializes logger factory for tests in this class.
        /// </summary>
        public BlockStoreTests()
        {
            // These tests use Network.Main.
            // Ensure that these static flags have the expected values.
            Block.BlockSignature = false;
            Transaction.TimeStamp = false;

            this.loggerFactory = new LoggerFactory();
            DBreezeSerializer serializer = new DBreezeSerializer();
            serializer.Initialize();
        }

        private void BlockRepositoryBench()
        {
            using (var dir = TestDirectory.Create())
            {
                using (var blockRepo = new BlockRepository(Network.Main, dir.FolderName, DateTimeProvider.Default, this.loggerFactory))
                {
                    var lst = new List<Block>();
                    for (int i = 0; i < 30; i++)
                    {
                        // roughly 1mb blocks
                        var block = new Block();
                        for (int j = 0; j < 3000; j++)
                        {
                            var trx = new Transaction();
                            block.AddTransaction(new Transaction());
                            trx.AddInput(new TxIn(Script.Empty));
                            trx.AddOutput(Money.COIN + j + i, new Script(Guid.NewGuid().ToByteArray()
                                .Concat(Guid.NewGuid().ToByteArray())
                                .Concat(Guid.NewGuid().ToByteArray())
                                .Concat(Guid.NewGuid().ToByteArray())
                                .Concat(Guid.NewGuid().ToByteArray())
                                .Concat(Guid.NewGuid().ToByteArray())));
                            trx.AddInput(new TxIn(Script.Empty));
                            trx.AddOutput(Money.COIN + j + i + 1, new Script(Guid.NewGuid().ToByteArray()
                                .Concat(Guid.NewGuid().ToByteArray())
                                .Concat(Guid.NewGuid().ToByteArray())
                                .Concat(Guid.NewGuid().ToByteArray())
                                .Concat(Guid.NewGuid().ToByteArray())
                                .Concat(Guid.NewGuid().ToByteArray())));
                            block.AddTransaction(trx);
                        }
                        block.UpdateMerkleRoot();
                        block.Header.HashPrevBlock = lst.Any() ? lst.Last().GetHash() : Network.Main.GenesisHash;
                        lst.Add(block);
                    }

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    blockRepo.PutAsync(lst.Last().GetHash(), lst).GetAwaiter().GetResult();
                    var first = stopwatch.ElapsedMilliseconds;
                    blockRepo.PutAsync(lst.Last().GetHash(), lst).GetAwaiter().GetResult();
                    var second = stopwatch.ElapsedMilliseconds;
                }
            }
        }

        [Fact]
        public void BlockRepositoryPutBatch()
        {
            using (var dir = TestDirectory.Create())
            {
                using (var blockRepo = new BlockRepository(Network.Main, dir.FolderName, DateTimeProvider.Default, this.loggerFactory))
                {
                    blockRepo.SetTxIndexAsync(true).Wait();

                    var lst = new List<Block>();
                    for (int i = 0; i < 5; i++)
                    {
                        // put
                        var block = new Block();
                        block.AddTransaction(new Transaction());
                        block.AddTransaction(new Transaction());
                        block.Transactions[0].AddInput(new TxIn(Script.Empty));
                        block.Transactions[0].AddOutput(Money.COIN + i * 2, Script.Empty);
                        block.Transactions[1].AddInput(new TxIn(Script.Empty));
                        block.Transactions[1].AddOutput(Money.COIN + i * 2 + 1, Script.Empty);
                        block.UpdateMerkleRoot();
                        block.Header.HashPrevBlock = lst.Any() ? lst.Last().GetHash() : Network.Main.GenesisHash;
                        lst.Add(block);
                    }

                    blockRepo.PutAsync(lst.Last().GetHash(), lst).GetAwaiter().GetResult();

                    // check each block
                    foreach (var block in lst)
                    {
                        var received = blockRepo.GetAsync(block.GetHash()).GetAwaiter().GetResult();
                        Assert.True(block.ToBytes().SequenceEqual(received.ToBytes()));

                        foreach (var transaction in block.Transactions)
                        {
                            var trx = blockRepo.GetTrxAsync(transaction.GetHash()).GetAwaiter().GetResult();
                            Assert.True(trx.ToBytes().SequenceEqual(transaction.ToBytes()));
                        }
                    }

                    // delete
                    blockRepo.DeleteAsync(lst.ElementAt(2).GetHash(), new[] { lst.ElementAt(2).GetHash() }.ToList()).GetAwaiter().GetResult();
                    var deleted = blockRepo.GetAsync(lst.ElementAt(2).GetHash()).GetAwaiter().GetResult();
                    Assert.Null(deleted);
                }
            }
        }

        [Fact]
        public void BlockRepositoryBlockHash()
        {
            using (var dir = TestDirectory.Create())
            {
                using (var blockRepo = new BlockRepository(Network.Main, dir.FolderName, DateTimeProvider.Default, this.loggerFactory))
                {
                    blockRepo.InitializeAsync().GetAwaiter().GetResult();

                    Assert.Equal(Network.Main.GenesisHash, blockRepo.BlockHash);
                    var hash = new Block().GetHash();
                    blockRepo.SetBlockHashAsync(hash).GetAwaiter().GetResult();
                    Assert.Equal(hash, blockRepo.BlockHash);
                }
            }
        }

        [Fact]
        public void BlockBroadcastInv()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var PurpleNodeSync = builder.CreatePurplePowNode();
                var PurpleNode1 = builder.CreatePurplePowNode();
                var PurpleNode2 = builder.CreatePurplePowNode();
                builder.StartAll();
                PurpleNodeSync.NotInIBD();
                PurpleNode1.NotInIBD();
                PurpleNode2.NotInIBD();

                // generate blocks and wait for the downloader to pickup
                PurpleNodeSync.SetDummyMinerSecret(new BitcoinSecret(new Key(), PurpleNodeSync.FullNode.Network));
                PurpleNodeSync.GeneratePurpleWithMiner(10); // coinbase maturity = 10
                // wait for block repo for block sync to work
                TestHelper.WaitLoop(() => PurpleNodeSync.FullNode.ConsensusLoop().Tip.HashBlock == PurpleNodeSync.FullNode.Chain.Tip.HashBlock);
                TestHelper.WaitLoop(() => PurpleNodeSync.FullNode.ChainBehaviorState.ConsensusTip.HashBlock == PurpleNodeSync.FullNode.Chain.Tip.HashBlock);
                TestHelper.WaitLoop(() => PurpleNodeSync.FullNode.HighestPersistedBlock().HashBlock == PurpleNodeSync.FullNode.Chain.Tip.HashBlock);

                // sync both nodes
                PurpleNode1.CreateRPCClient().AddNode(PurpleNodeSync.Endpoint, true);
                PurpleNode2.CreateRPCClient().AddNode(PurpleNodeSync.Endpoint, true);
                TestHelper.WaitLoop(() => PurpleNode1.CreateRPCClient().GetBestBlockHash() == PurpleNodeSync.CreateRPCClient().GetBestBlockHash());
                TestHelper.WaitLoop(() => PurpleNode2.CreateRPCClient().GetBestBlockHash() == PurpleNodeSync.CreateRPCClient().GetBestBlockHash());

                // set node2 to use inv (not headers)
                PurpleNode2.FullNode.ConnectionManager.ConnectedPeers.First().Behavior<BlockStoreBehavior>().PreferHeaders = false;

                // generate two new blocks
                PurpleNodeSync.GeneratePurpleWithMiner(2);
                // wait for block repo for block sync to work
                TestHelper.WaitLoop(() => PurpleNodeSync.FullNode.Chain.Tip.HashBlock == PurpleNodeSync.FullNode.ConsensusLoop().Tip.HashBlock);
                TestHelper.WaitLoop(() => PurpleNodeSync.FullNode.BlockStoreManager().BlockRepository.GetAsync(PurpleNodeSync.CreateRPCClient().GetBestBlockHash()).Result != null);

                // wait for the other nodes to pick up the newly generated blocks
                TestHelper.WaitLoop(() => PurpleNode1.CreateRPCClient().GetBestBlockHash() == PurpleNodeSync.CreateRPCClient().GetBestBlockHash());
                TestHelper.WaitLoop(() => PurpleNode2.CreateRPCClient().GetBestBlockHash() == PurpleNodeSync.CreateRPCClient().GetBestBlockHash());
            }
        }

        [Fact]
        public void BlockStoreCanRecoverOnStartup()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var PurpleNodeSync = builder.CreatePurplePowNode();
                builder.StartAll();
                PurpleNodeSync.NotInIBD();

                // generate blocks and wait for the downloader to pickup
                PurpleNodeSync.SetDummyMinerSecret(new BitcoinSecret(new Key(), PurpleNodeSync.FullNode.Network));

                PurpleNodeSync.GeneratePurpleWithMiner(10);
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(PurpleNodeSync));

                // set the tip of best chain some blocks in the apst
                PurpleNodeSync.FullNode.Chain.SetTip(PurpleNodeSync.FullNode.Chain.GetBlock(PurpleNodeSync.FullNode.Chain.Height - 5));

                // stop the node it will persist the chain with the reset tip
                PurpleNodeSync.FullNode.Dispose();

                var newNodeInstance = builder.ClonePurpleNode(PurpleNodeSync);

                // load the node, this should hit the block store recover code
                newNodeInstance.Start();

                // check that store recovered to be the same as the best chain.
                Assert.Equal(newNodeInstance.FullNode.Chain.Tip.HashBlock, newNodeInstance.FullNode.HighestPersistedBlock().HashBlock);
                //TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(PurpleNodeSync));
            }
        }

        [Fact]
        public void BlockStoreCanReorg()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var PurpleNodeSync = builder.CreatePurplePowNode();
                var PurpleNode1 = builder.CreatePurplePowNode();
                var PurpleNode2 = builder.CreatePurplePowNode();
                builder.StartAll();
                PurpleNodeSync.NotInIBD();
                PurpleNode1.NotInIBD();
                PurpleNode2.NotInIBD();

                // generate blocks and wait for the downloader to pickup
                PurpleNode1.SetDummyMinerSecret(new BitcoinSecret(new Key(), PurpleNodeSync.FullNode.Network));
                PurpleNode2.SetDummyMinerSecret(new BitcoinSecret(new Key(), PurpleNodeSync.FullNode.Network));
                // sync both nodes
                PurpleNodeSync.CreateRPCClient().AddNode(PurpleNode1.Endpoint, true);
                PurpleNodeSync.CreateRPCClient().AddNode(PurpleNode2.Endpoint, true);

                PurpleNode1.GeneratePurpleWithMiner(10);
                TestHelper.WaitLoop(() => PurpleNode1.FullNode.HighestPersistedBlock().Height == 10);

                TestHelper.WaitLoop(() => PurpleNode1.FullNode.HighestPersistedBlock().HashBlock == PurpleNodeSync.FullNode.HighestPersistedBlock().HashBlock);
                TestHelper.WaitLoop(() => PurpleNode2.FullNode.HighestPersistedBlock().HashBlock == PurpleNodeSync.FullNode.HighestPersistedBlock().HashBlock);

                // remove node 2
                PurpleNodeSync.CreateRPCClient().RemoveNode(PurpleNode2.Endpoint);

                // mine some more with node 1
                PurpleNode1.GeneratePurpleWithMiner(10);

                // wait for node 1 to sync
                TestHelper.WaitLoop(() => PurpleNode1.FullNode.HighestPersistedBlock().Height == 20);
                TestHelper.WaitLoop(() => PurpleNode1.FullNode.HighestPersistedBlock().HashBlock == PurpleNodeSync.FullNode.HighestPersistedBlock().HashBlock);

                // remove node 1
                PurpleNodeSync.CreateRPCClient().RemoveNode(PurpleNode1.Endpoint);

                // mine a higher chain with node2
                PurpleNode2.GeneratePurpleWithMiner(20);
                TestHelper.WaitLoop(() => PurpleNode2.FullNode.HighestPersistedBlock().Height == 30);

                // add node2
                PurpleNodeSync.CreateRPCClient().AddNode(PurpleNode2.Endpoint, true);

                // node2 should be synced
                TestHelper.WaitLoop(() => PurpleNode2.FullNode.HighestPersistedBlock().HashBlock == PurpleNodeSync.FullNode.HighestPersistedBlock().HashBlock);
            }
        }

        [Fact]
        public void BlockStoreIndexTx()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var PurpleNode1 = builder.CreatePurplePowNode();
                var PurpleNode2 = builder.CreatePurplePowNode();
                builder.StartAll();
                PurpleNode1.NotInIBD();
                PurpleNode2.NotInIBD();

                // generate blocks and wait for the downloader to pickup
                PurpleNode1.SetDummyMinerSecret(new BitcoinSecret(new Key(), PurpleNode1.FullNode.Network));
                PurpleNode2.SetDummyMinerSecret(new BitcoinSecret(new Key(), PurpleNode2.FullNode.Network));
                // sync both nodes
                PurpleNode1.CreateRPCClient().AddNode(PurpleNode2.Endpoint, true);
                PurpleNode1.GeneratePurpleWithMiner(10);
                TestHelper.WaitLoop(() => PurpleNode1.FullNode.HighestPersistedBlock().Height == 10);
                TestHelper.WaitLoop(() => PurpleNode1.FullNode.HighestPersistedBlock().HashBlock == PurpleNode2.FullNode.HighestPersistedBlock().HashBlock);

                var bestBlock1 = PurpleNode1.FullNode.BlockStoreManager().BlockRepository.GetAsync(PurpleNode1.FullNode.Chain.Tip.HashBlock).Result;
                Assert.NotNull(bestBlock1);

                // get the block coinbase trx
                var trx = PurpleNode2.FullNode.BlockStoreManager().BlockRepository.GetTrxAsync(bestBlock1.Transactions.First().GetHash()).Result;
                Assert.NotNull(trx);
                Assert.Equal(bestBlock1.Transactions.First().GetHash(), trx.GetHash());
            }
        }
    }
}
