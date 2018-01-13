using Microsoft.Extensions.DependencyInjection;
using Purple.Bitcoin.Builder;
using Purple.Bitcoin.Builder.Feature;
using Purple.Bitcoin.Connection;
using Purple.Bitcoin.Features.Notifications.Controllers;

namespace Purple.Bitcoin.Features.Notifications
{
    /// <summary>
    /// Feature enabling the broadcasting of transactions.
    /// </summary>
    public class TransactionNotificationFeature : FullNodeFeature
    {
        private readonly IConnectionManager connectionManager;

        private readonly TransactionReceiver transactionBehavior;

        public TransactionNotificationFeature(IConnectionManager connectionManager, TransactionReceiver transactionBehavior)
        {
            this.connectionManager = connectionManager;
            this.transactionBehavior = transactionBehavior;
        }

        public override void Initialize()
        {
            this.connectionManager.Parameters.TemplateBehaviors.Add(this.transactionBehavior);
        }
    }

    /// <summary>
    /// A class providing extension methods for <see cref="IFullNodeBuilder"/>.
    /// </summary>
    public static class FullNodeBuilderTransactionNotificationExtension
    {
        public static IFullNodeBuilder UseTransactionNotification(this IFullNodeBuilder fullNodeBuilder)
        {
            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                .AddFeature<TransactionNotificationFeature>()
                .FeatureServices(services =>
                    {
                        services.AddSingleton<TransactionNotificationProgress>();
                        services.AddSingleton<TransactionNotification>();
                        services.AddSingleton<TransactionReceiver>();
                        services.AddSingleton<NotificationsController>();
                    });
            });

            return fullNodeBuilder;
        }
    }
}
