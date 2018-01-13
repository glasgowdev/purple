using System.Collections.Generic;
using System.IO;
using System.Linq;
using Purple.Bitcoin.IntegrationTests.EnvironmentMockUpHelpers;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
using Purple.Bitcoin.Features.Consensus;
using Purple.Bitcoin.Features.Wallet;
using Purple.Bitcoin.Features.Wallet.Controllers;
using Purple.Bitcoin.Features.Wallet.Interfaces;
using Purple.Bitcoin.Features.Wallet.Models;
using Xunit;

namespace Purple.Bitcoin.IntegrationTests
{
    public class WalletTests
    {
        [Fact]
        public void WalletCanReceiveAndSendCorrectly()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var PurpleSender = builder.CreatePurplePowNode();
                var PurpleReceiver = builder.CreatePurplePowNode();

                builder.StartAll();
                PurpleSender.NotInIBD();
                PurpleReceiver.NotInIBD();

                // get a key from the wallet
                var mnemonic1 = PurpleSender.FullNode.WalletManager().CreateWallet("123456", "mywallet");
                var mnemonic2 = PurpleReceiver.FullNode.WalletManager().CreateWallet("123456", "mywallet");
                Assert.Equal(12, mnemonic1.Words.Length);
                Assert.Equal(12, mnemonic2.Words.Length);
                var addr = PurpleSender.FullNode.WalletManager().GetUnusedAddress(new WalletAccountReference("mywallet", "account 0"));
                var wallet = PurpleSender.FullNode.WalletManager().GetWalletByName("mywallet");
                var key = wallet.GetExtendedPrivateKeyForAddress("123456", addr).PrivateKey;

                PurpleSender.SetDummyMinerSecret(new BitcoinSecret(key, PurpleSender.FullNode.Network));
                var maturity = (int)PurpleSender.FullNode.Network.Consensus.Option<PowConsensusOptions>().CoinbaseMaturity;
                PurpleSender.GeneratePurple(maturity + 5);
                // wait for block repo for block sync to work

                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(PurpleSender));

                // the mining should add coins to the wallet
                var total = PurpleSender.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Sum(s => s.Transaction.Amount);
                Assert.Equal(Money.COIN * 105 * 50, total);

                // sync both nodes
                PurpleSender.CreateRPCClient().AddNode(PurpleReceiver.Endpoint, true);
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleReceiver, PurpleSender));

                // send coins to the receiver
                var sendto = PurpleReceiver.FullNode.WalletManager().GetUnusedAddress(new WalletAccountReference("mywallet", "account 0"));
                var trx = PurpleSender.FullNode.WalletTransactionHandler().BuildTransaction(CreateContext(
                    new WalletAccountReference("mywallet", "account 0"), "123456", sendto.ScriptPubKey, Money.COIN * 100, FeeType.Medium, 101));

                // broadcast to the other node
                PurpleSender.FullNode.NodeService<WalletController>().SendTransaction(new SendTransactionRequest(trx.ToHex()));

                // wait for the trx to arrive
                TestHelper.WaitLoop(() => PurpleReceiver.CreateRPCClient().GetRawMempool().Length > 0);
                TestHelper.WaitLoop(() => PurpleReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Any());

                var receivetotal = PurpleReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Sum(s => s.Transaction.Amount);
                Assert.Equal(Money.COIN * 100, receivetotal);
                Assert.Null(PurpleReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").First().Transaction.BlockHeight);

                // generate two new blocks do the trx is confirmed
                PurpleSender.GeneratePurple(1, new List<Transaction>(new[] { trx.Clone() }));
                PurpleSender.GeneratePurple(1);

                // wait for block repo for block sync to work
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(PurpleSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleReceiver, PurpleSender));

                TestHelper.WaitLoop(() => maturity + 6 == PurpleReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").First().Transaction.BlockHeight);
            }
        }

        [Fact]
        public void CanMineAndSendToAddress()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                CoreNode PurpleNodeSync = builder.CreatePurplePowNode();
                this.InitializeTestWallet(PurpleNodeSync);
                builder.StartAll();
                var rpc = PurpleNodeSync.CreateRPCClient();
                rpc.SendCommand(NBitcoin.RPC.RPCOperations.generate, 10);
                Assert.Equal(10, rpc.GetBlockCount());

                var address = new Key().PubKey.GetAddress(rpc.Network);
                var tx = rpc.SendToAddress(address, Money.Coins(1.0m));
                Assert.NotNull(tx);
            }
        }

        [Fact]
        public void WalletCanReorg()
        {
            // this test has 4 parts:
            // send first transaction from one wallet to another and wait for it to be confirmed
            // send a second transaction and wait for it to be confirmed
            // connected to a longer chain that couse a reorg back so the second trasnaction is undone
            // mine the second transaction back in to the main chain

            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var PurpleSender = builder.CreatePurplePowNode();
                var PurpleReceiver = builder.CreatePurplePowNode();
                var PurpleReorg = builder.CreatePurplePowNode();

                builder.StartAll();
                PurpleSender.NotInIBD();
                PurpleReceiver.NotInIBD();
                PurpleReorg.NotInIBD();

                // get a key from the wallet
                var mnemonic1 = PurpleSender.FullNode.WalletManager().CreateWallet("123456", "mywallet");
                var mnemonic2 = PurpleReceiver.FullNode.WalletManager().CreateWallet("123456", "mywallet");
                Assert.Equal(12, mnemonic1.Words.Length);
                Assert.Equal(12, mnemonic2.Words.Length);
                var addr = PurpleSender.FullNode.WalletManager().GetUnusedAddress(new WalletAccountReference("mywallet", "account 0"));
                var wallet = PurpleSender.FullNode.WalletManager().GetWalletByName("mywallet");
                var key = wallet.GetExtendedPrivateKeyForAddress("123456", addr).PrivateKey;

                PurpleSender.SetDummyMinerSecret(new BitcoinSecret(key, PurpleSender.FullNode.Network));
                PurpleReorg.SetDummyMinerSecret(new BitcoinSecret(key, PurpleSender.FullNode.Network));

                var maturity = (int)PurpleSender.FullNode.Network.Consensus.Option<PowConsensusOptions>().CoinbaseMaturity;
                PurpleSender.GeneratePurpleWithMiner(maturity + 15);

                var currentBestHeight = maturity + 15;

                // wait for block repo for block sync to work
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(PurpleSender));

                // the mining should add coins to the wallet
                var total = PurpleSender.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Sum(s => s.Transaction.Amount);
                Assert.Equal(Money.COIN * currentBestHeight * 50, total);

                // sync all nodes
                PurpleReceiver.CreateRPCClient().AddNode(PurpleSender.Endpoint, true);
                PurpleReceiver.CreateRPCClient().AddNode(PurpleReorg.Endpoint, true);
                PurpleSender.CreateRPCClient().AddNode(PurpleReorg.Endpoint, true);
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleReceiver, PurpleSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleReceiver, PurpleReorg));

                // Build Transaction 1
                // ====================
                // send coins to the receiver
                var sendto = PurpleReceiver.FullNode.WalletManager().GetUnusedAddress(new WalletAccountReference("mywallet", "account 0"));
                var transaction1 = PurpleSender.FullNode.WalletTransactionHandler().BuildTransaction(CreateContext(new WalletAccountReference("mywallet", "account 0"), "123456", sendto.ScriptPubKey, Money.COIN * 100, FeeType.Medium, 101));

                // broadcast to the other node
                PurpleSender.FullNode.NodeService<WalletController>().SendTransaction(new SendTransactionRequest(transaction1.ToHex()));

                // wait for the trx to arrive
                TestHelper.WaitLoop(() => PurpleReceiver.CreateRPCClient().GetRawMempool().Length > 0);
                Assert.NotNull(PurpleReceiver.CreateRPCClient().GetRawTransaction(transaction1.GetHash(), false));
                TestHelper.WaitLoop(() => PurpleReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Any());

                var receivetotal = PurpleReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Sum(s => s.Transaction.Amount);
                Assert.Equal(Money.COIN * 100, receivetotal);
                Assert.Null(PurpleReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").First().Transaction.BlockHeight);

                // generate two new blocks so the trx is confirmed
                PurpleSender.GeneratePurpleWithMiner(1);
                var transaction1MinedHeight = currentBestHeight + 1;
                PurpleSender.GeneratePurpleWithMiner(1);
                currentBestHeight = currentBestHeight + 2;

                // wait for block repo for block sync to work
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(PurpleSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleReceiver, PurpleSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleReceiver, PurpleReorg));
                Assert.Equal(currentBestHeight, PurpleReceiver.FullNode.Chain.Tip.Height);
                TestHelper.WaitLoop(() => transaction1MinedHeight == PurpleReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").First().Transaction.BlockHeight);

                // Build Transaction 2
                // ====================
                // remove the reorg node
                PurpleReceiver.CreateRPCClient().RemoveNode(PurpleReorg.Endpoint);
                PurpleSender.CreateRPCClient().RemoveNode(PurpleReorg.Endpoint);
                var forkblock = PurpleReceiver.FullNode.Chain.Tip;

                // send more coins to the wallet
                sendto = PurpleReceiver.FullNode.WalletManager().GetUnusedAddress(new WalletAccountReference("mywallet", "account 0"));
                var transaction2 = PurpleSender.FullNode.WalletTransactionHandler().BuildTransaction(CreateContext(new WalletAccountReference("mywallet", "account 0"), "123456", sendto.ScriptPubKey, Money.COIN * 10, FeeType.Medium, 101));
                PurpleSender.FullNode.NodeService<WalletController>().SendTransaction(new SendTransactionRequest(transaction2.ToHex()));
                // wait for the trx to arrive
                TestHelper.WaitLoop(() => PurpleReceiver.CreateRPCClient().GetRawMempool().Length > 0);
                Assert.NotNull(PurpleReceiver.CreateRPCClient().GetRawTransaction(transaction2.GetHash(), false));
                TestHelper.WaitLoop(() => PurpleReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Any());
                var newamount = PurpleReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Sum(s => s.Transaction.Amount);
                Assert.Equal(Money.COIN * 110, newamount);
                Assert.Contains(PurpleReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet"), b => b.Transaction.BlockHeight == null);

                // mine more blocks so its included in the chain

                PurpleSender.GeneratePurpleWithMiner(1);
                var transaction2MinedHeight = currentBestHeight + 1;
                PurpleSender.GeneratePurpleWithMiner(1);
                currentBestHeight = currentBestHeight + 2;
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(PurpleSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleReceiver, PurpleSender));
                Assert.Equal(currentBestHeight, PurpleReceiver.FullNode.Chain.Tip.Height);
                TestHelper.WaitLoop(() => PurpleReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Any(b => b.Transaction.BlockHeight == transaction2MinedHeight));

                // create a reorg by mining on two different chains
                // ================================================
                // advance both chains, one chin is longer
                PurpleSender.GeneratePurpleWithMiner(2);
                PurpleReorg.GeneratePurpleWithMiner(10);
                currentBestHeight = forkblock.Height + 10;
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(PurpleSender));
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(PurpleReorg));

                // connect the reorg chain
                PurpleReceiver.CreateRPCClient().AddNode(PurpleReorg.Endpoint, true);
                PurpleSender.CreateRPCClient().AddNode(PurpleReorg.Endpoint, true);
                // wait for the chains to catch up
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleReceiver, PurpleSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleReceiver, PurpleReorg));
                Assert.Equal(currentBestHeight, PurpleReceiver.FullNode.Chain.Tip.Height);

                // ensure wallet reorg complete
                TestHelper.WaitLoop(() => PurpleReceiver.FullNode.WalletManager().WalletTipHash == PurpleReorg.CreateRPCClient().GetBestBlockHash());
                // check the wallet amount was rolled back
                var newtotal = PurpleReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Sum(s => s.Transaction.Amount);
                Assert.Equal(receivetotal, newtotal);
                TestHelper.WaitLoop(() => maturity + 16 == PurpleReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").First().Transaction.BlockHeight);

                // ReBuild Transaction 2
                // ====================
                // After the reorg transaction2 was returned back to mempool
                PurpleSender.FullNode.NodeService<WalletController>().SendTransaction(new SendTransactionRequest(transaction2.ToHex()));

                TestHelper.WaitLoop(() => PurpleReceiver.CreateRPCClient().GetRawMempool().Length > 0);
                // mine the transaction again
                PurpleSender.GeneratePurpleWithMiner(1);
                transaction2MinedHeight = currentBestHeight + 1;
                PurpleSender.GeneratePurpleWithMiner(1);
                currentBestHeight = currentBestHeight + 2;

                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(PurpleSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleReceiver, PurpleSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleReceiver, PurpleReorg));
                Assert.Equal(currentBestHeight, PurpleReceiver.FullNode.Chain.Tip.Height);
                var newsecondamount = PurpleReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Sum(s => s.Transaction.Amount);
                Assert.Equal(newamount, newsecondamount);
                TestHelper.WaitLoop(() => PurpleReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Any(b => b.Transaction.BlockHeight == transaction2MinedHeight));
            }
        }

        [Fact]
        public void Given__TheNodeHadAReorg_And_WalletTipIsBehindConsensusTip__When__ANewBlockArrives__Then__WalletCanRecover()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var PurpleSender = builder.CreatePurplePowNode();
                var PurpleReceiver = builder.CreatePurplePowNode();
                var PurpleReorg = builder.CreatePurplePowNode();

                builder.StartAll();
                PurpleSender.NotInIBD();
                PurpleReceiver.NotInIBD();
                PurpleReorg.NotInIBD();

                PurpleSender.SetDummyMinerSecret(new BitcoinSecret(new Key(), PurpleSender.FullNode.Network));
                PurpleReorg.SetDummyMinerSecret(new BitcoinSecret(new Key(), PurpleReorg.FullNode.Network));

                PurpleSender.GeneratePurpleWithMiner(10);

                // wait for block repo for block sync to work
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(PurpleSender));

                //// sync all nodes
                PurpleReceiver.CreateRPCClient().AddNode(PurpleSender.Endpoint, true);
                PurpleReceiver.CreateRPCClient().AddNode(PurpleReorg.Endpoint, true);
                PurpleSender.CreateRPCClient().AddNode(PurpleReorg.Endpoint, true);
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleReceiver, PurpleSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleReceiver, PurpleReorg));

                // remove the reorg node
                PurpleReceiver.CreateRPCClient().RemoveNode(PurpleReorg.Endpoint);
                PurpleSender.CreateRPCClient().RemoveNode(PurpleReorg.Endpoint);

                // create a reorg by mining on two different chains
                // ================================================
                // advance both chains, one chin is longer
                PurpleSender.GeneratePurpleWithMiner(2);
                PurpleReorg.GeneratePurpleWithMiner(10);
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(PurpleSender));
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(PurpleReorg));

                // rewind the wallet in the PurpleReceiver node
                (PurpleReceiver.FullNode.NodeService<IWalletSyncManager>() as WalletSyncManager).SyncFromHeight(5);

                // connect the reorg chain
                PurpleReceiver.CreateRPCClient().AddNode(PurpleReorg.Endpoint, true);
                PurpleSender.CreateRPCClient().AddNode(PurpleReorg.Endpoint, true);
                // wait for the chains to catch up
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleReceiver, PurpleSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleReceiver, PurpleReorg));
                Assert.Equal(20, PurpleReceiver.FullNode.Chain.Tip.Height);

                PurpleSender.GeneratePurpleWithMiner(5);

                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleReceiver, PurpleSender));
                Assert.Equal(25, PurpleReceiver.FullNode.Chain.Tip.Height);
            }
        }

        [Fact]
        public void Given__TheNodeHadAReorg_And_ConensusTipIsdifferentFromWalletTip__When__ANewBlockArrives__Then__WalletCanRecover()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var PurpleSender = builder.CreatePurplePowNode();
                var PurpleReceiver = builder.CreatePurplePowNode();
                var PurpleReorg = builder.CreatePurplePowNode();

                builder.StartAll();
                PurpleSender.NotInIBD();
                PurpleReceiver.NotInIBD();
                PurpleReorg.NotInIBD();

                PurpleSender.SetDummyMinerSecret(new BitcoinSecret(new Key(), PurpleSender.FullNode.Network));
                PurpleReorg.SetDummyMinerSecret(new BitcoinSecret(new Key(), PurpleReorg.FullNode.Network));

                PurpleSender.GeneratePurpleWithMiner(10);

                // wait for block repo for block sync to work
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(PurpleSender));

                //// sync all nodes
                PurpleReceiver.CreateRPCClient().AddNode(PurpleSender.Endpoint, true);
                PurpleReceiver.CreateRPCClient().AddNode(PurpleReorg.Endpoint, true);
                PurpleSender.CreateRPCClient().AddNode(PurpleReorg.Endpoint, true);
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleReceiver, PurpleSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleReceiver, PurpleReorg));

                // remove the reorg node
                PurpleReceiver.CreateRPCClient().RemoveNode(PurpleReorg.Endpoint);
                PurpleSender.CreateRPCClient().RemoveNode(PurpleReorg.Endpoint);

                // create a reorg by mining on two different chains
                // ================================================
                // advance both chains, one chin is longer
                PurpleSender.GeneratePurpleWithMiner(2);
                PurpleReorg.GeneratePurpleWithMiner(10);
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(PurpleSender));
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(PurpleReorg));

                // connect the reorg chain
                PurpleReceiver.CreateRPCClient().AddNode(PurpleReorg.Endpoint, true);
                PurpleSender.CreateRPCClient().AddNode(PurpleReorg.Endpoint, true);
                // wait for the chains to catch up
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleReceiver, PurpleSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleReceiver, PurpleReorg));
                Assert.Equal(20, PurpleReceiver.FullNode.Chain.Tip.Height);

                // rewind the wallet in the PurpleReceiver node
                (PurpleReceiver.FullNode.NodeService<IWalletSyncManager>() as WalletSyncManager).SyncFromHeight(10);

                PurpleSender.GeneratePurpleWithMiner(5);

                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(PurpleReceiver, PurpleSender));
                Assert.Equal(25, PurpleReceiver.FullNode.Chain.Tip.Height);
            }
        }

        [Fact]
        public void WalletCanCatchupWithBestChain()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var Purpleminer = builder.CreatePurplePowNode();

                builder.StartAll();
                Purpleminer.NotInIBD();

                // get a key from the wallet
                var mnemonic = Purpleminer.FullNode.WalletManager().CreateWallet("123456", "mywallet");
                Assert.Equal(12, mnemonic.Words.Length);
                var addr = Purpleminer.FullNode.WalletManager().GetUnusedAddress(new WalletAccountReference("mywallet", "account 0"));
                var wallet = Purpleminer.FullNode.WalletManager().GetWalletByName("mywallet");
                var key = wallet.GetExtendedPrivateKeyForAddress("123456", addr).PrivateKey;

                Purpleminer.SetDummyMinerSecret(key.GetBitcoinSecret(Purpleminer.FullNode.Network));
                Purpleminer.GeneratePurple(10);
                // wait for block repo for block sync to work
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(Purpleminer));

                // push the wallet back
                Purpleminer.FullNode.Services.ServiceProvider.GetService<IWalletSyncManager>().SyncFromHeight(5);

                Purpleminer.GeneratePurple(5);

                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(Purpleminer));
            }
        }

        [Fact]
        public void WalletCanRecoverOnStartup()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var PurpleNodeSync = builder.CreatePurplePowNode();
                builder.StartAll();
                PurpleNodeSync.NotInIBD();

                // get a key from the wallet
                var mnemonic = PurpleNodeSync.FullNode.WalletManager().CreateWallet("123456", "mywallet");
                Assert.Equal(12, mnemonic.Words.Length);
                var addr = PurpleNodeSync.FullNode.WalletManager().GetUnusedAddress(new WalletAccountReference("mywallet", "account 0"));
                var wallet = PurpleNodeSync.FullNode.WalletManager().GetWalletByName("mywallet");
                var key = wallet.GetExtendedPrivateKeyForAddress("123456", addr).PrivateKey;

                PurpleNodeSync.SetDummyMinerSecret(key.GetBitcoinSecret(PurpleNodeSync.FullNode.Network));
                PurpleNodeSync.GeneratePurple(10);
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(PurpleNodeSync));

                // set the tip of best chain some blocks in the apst
                PurpleNodeSync.FullNode.Chain.SetTip(PurpleNodeSync.FullNode.Chain.GetBlock(PurpleNodeSync.FullNode.Chain.Height - 5));

                // stop the node it will persist the chain with the reset tip
                PurpleNodeSync.FullNode.Dispose();

                var newNodeInstance = builder.ClonePurpleNode(PurpleNodeSync);

                // load the node, this should hit the block store recover code
                newNodeInstance.Start();

                // check that store recovered to be the same as the best chain.
                Assert.Equal(newNodeInstance.FullNode.Chain.Tip.HashBlock, newNodeInstance.FullNode.WalletManager().WalletTipHash);
            }
        }

        public static TransactionBuildContext CreateContext(WalletAccountReference accountReference, string password,
            Script destinationScript, Money amount, FeeType feeType, int minConfirmations)
        {
            return new TransactionBuildContext(accountReference,
                new[] { new Recipient { Amount = amount, ScriptPubKey = destinationScript } }.ToList(), password)
            {
                MinConfirmations = minConfirmations,
                FeeType = feeType
            };
        }

        /// <summary>
        /// Copies the test wallet into data folder for node if it isnt' already present.
        /// </summary>
        /// <param name="node">Core node for the test.</param>
        private void InitializeTestWallet(CoreNode node)
        {
            string testWalletPath = Path.Combine(node.DataFolder, "test.wallet.json");
            if (!File.Exists(testWalletPath))
                File.Copy("Data/test.wallet.json", testWalletPath);
        }
    }
}