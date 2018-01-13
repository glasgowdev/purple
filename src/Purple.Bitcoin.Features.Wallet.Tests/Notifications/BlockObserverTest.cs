using Moq;
using NBitcoin;
using Purple.Bitcoin.Features.Wallet.Interfaces;
using Purple.Bitcoin.Features.Wallet.Notifications;
using Xunit;

namespace Purple.Bitcoin.Features.Wallet.Tests.Notifications
{
    public class BlockObserverTest
    {
        [Fact]
        public void OnNextCoreProcessesOnTheWalletSyncManager()
        {
            var walletSyncManager = new Mock<IWalletSyncManager>();
            BlockObserver observer = new BlockObserver(walletSyncManager.Object);
            Block block = new Block();

            observer.OnNext(block);

            walletSyncManager.Verify(w => w.ProcessBlock(block), Times.Exactly(1));
        }
    }
}
