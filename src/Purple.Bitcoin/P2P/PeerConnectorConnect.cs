﻿using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.Protocol;
using Purple.Bitcoin.Configuration;
using Purple.Bitcoin.P2P.Peer;
using Purple.Bitcoin.P2P.Protocol.Payloads;
using Purple.Bitcoin.Utilities;
using Purple.Bitcoin.Utilities.Extensions;

namespace Purple.Bitcoin.P2P
{
    /// <summary>
    /// The connector used to connect to peers specified with the -connect argument
    /// </summary>
    public sealed class PeerConnectorConnectNode : PeerConnector
    {
        /// <summary>Constructor for dependency injection.</summary>
        public PeerConnectorConnectNode(
            IAsyncLoopFactory asyncLoopFactory,
            IDateTimeProvider dateTimeProvider,
            ILoggerFactory loggerFactory,
            Network network,
            INetworkPeerFactory networkPeerFactory,
            INodeLifetime nodeLifetime,
            NodeSettings nodeSettings,
            IPeerAddressManager peerAddressManager) :
            base(asyncLoopFactory, dateTimeProvider, loggerFactory, network, networkPeerFactory, nodeLifetime, nodeSettings, peerAddressManager)
        {
            this.Requirements.RequiredServices = NetworkPeerServices.Nothing;
        }

        /// <inheritdoc/>
        public override void OnInitialize()
        {
            this.MaxOutboundConnections = this.NodeSettings.ConnectionManager.Connect.Count;

            // Add the endpoints from the -connect arg to the address manager
            foreach (var ipEndpoint in this.NodeSettings.ConnectionManager.Connect)
            {
                this.peerAddressManager.AddPeer(new NetworkAddress(ipEndpoint.MapToIpv6()), IPAddress.Loopback);
            }
        }

        /// <summary>This connector is only started if there are peers in the -connect args.</summary>
        public override bool CanStartConnect
        {
            get { return this.NodeSettings.ConnectionManager.Connect.Any(); }
        }

        /// <inheritdoc/>
        public override void OnStartConnect()
        {
            this.CurrentParameters.PeerAddressManagerBehaviour().Mode = PeerAddressManagerBehaviourMode.None;
        }

        /// <summary>
        /// Only connect to nodes as specified in the -connect node arg.
        /// </summary>
        public override async Task OnConnectAsync()
        {
            foreach (var ipEndpoint in this.NodeSettings.ConnectionManager.Connect)
            {
                if (this.nodeLifetime.ApplicationStopping.IsCancellationRequested)
                    return;

                PeerAddress peerAddress = this.peerAddressManager.FindPeer(ipEndpoint);
                if (peerAddress != null && !this.IsPeerConnected(peerAddress.NetworkAddress.Endpoint))
                    await ConnectAsync(peerAddress).ConfigureAwait(false);
            }
        }
    }
}