using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Purple.Bitcoin.Base;
using Purple.Bitcoin.BlockPulling;
using Purple.Bitcoin.Builder;
using Purple.Bitcoin.Builder.Feature;
using Purple.Bitcoin.Connection;
using Purple.Bitcoin.Features.Notifications.Controllers;
using Purple.Bitcoin.Features.Notifications.Interfaces;

[assembly: InternalsVisibleTo("Purple.Bitcoin.Features.Notifications.Tests")]

namespace Purple.Bitcoin.Features.Notifications
{
    /// <summary>
    /// Feature enabling the broadcasting of blocks.
    /// </summary>
    public class BlockNotificationFeature : FullNodeFeature
    {
        private readonly IBlockNotification blockNotification;

        private readonly IConnectionManager connectionManager;

        private readonly LookaheadBlockPuller blockPuller;

        private readonly ChainState chainState;

        private readonly ConcurrentChain chain;

        private readonly ILoggerFactory loggerFactory;

        public BlockNotificationFeature(IBlockNotification blockNotification, IConnectionManager connectionManager,
            LookaheadBlockPuller blockPuller, ChainState chainState, ConcurrentChain chain, ILoggerFactory loggerFactory)
        {
            this.blockNotification = blockNotification;
            this.connectionManager = connectionManager;
            this.blockPuller = blockPuller;
            this.chainState = chainState;
            this.chain = chain;
            this.loggerFactory = loggerFactory;
        }

        public override void Initialize()
        {
            var connectionParameters = this.connectionManager.Parameters;
            connectionParameters.TemplateBehaviors.Add(new BlockPullerBehavior(this.blockPuller, this.loggerFactory));

            this.blockNotification.Start();
            this.chainState.ConsensusTip = this.chain.Tip;
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            this.blockNotification.Stop();
        }
    }

    /// <summary>
    /// A class providing extension methods for <see cref="IFullNodeBuilder"/>.
    /// </summary>
    public static class FullNodeBuilderBlockNotificationExtension
    {
        public static IFullNodeBuilder UseBlockNotification(this IFullNodeBuilder fullNodeBuilder)
        {
            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                .AddFeature<BlockNotificationFeature>()
                .FeatureServices(services =>
                {
                    services.AddSingleton<IBlockNotification, BlockNotification>();
                    services.AddSingleton<LookaheadBlockPuller>().AddSingleton<ILookaheadBlockPuller, LookaheadBlockPuller>(provider => provider.GetService<LookaheadBlockPuller>());
                    services.AddSingleton<NotificationsController>();
                });
            });

            return fullNodeBuilder;
        }
    }
}
