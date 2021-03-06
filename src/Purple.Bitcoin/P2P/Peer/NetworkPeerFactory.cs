﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.Protocol;
using Purple.Bitcoin.P2P.Protocol;
using Purple.Bitcoin.P2P.Protocol.Payloads;
using Purple.Bitcoin.Utilities;

namespace Purple.Bitcoin.P2P.Peer
{
    /// <summary>
    /// Contract for factory for creating P2P network peers.
    /// </summary>
    public interface INetworkPeerFactory
    {
        /// <summary>
        /// Creates a network peer using already established network connection.
        /// </summary>
        /// <param name="network">The network to connect to.</param>
        /// <param name="client">Already connected network client.</param>
        /// <param name="parameters">Parameters of the established connection, or <c>null</c> to use default parameters.</param>
        /// <returns>New network peer that is connected via the established connection.</returns>
        NetworkPeer CreateNetworkPeer(Network network, TcpClient client, NetworkPeerConnectionParameters parameters = null);

        /// <summary>
        /// Creates a new network peer which is connected to a specified counterparty.
        /// </summary>
        /// <param name="network">The network to connect to.</param>
        /// <param name="endPoint">Address and port of the counterparty to connect to.</param>
        /// <param name="myVersion">Version of the protocol that the node supports.</param>
        /// <param name="isRelay">Whether the remote peer should announce relayed transactions or not. See <see cref="VersionPayload.Relay"/> for more information.</param>
        /// <param name="cancellation">Cancallation token that allows to interrupt establishing of the connection.</param>
        /// <returns>Network peer connected to the specified counterparty.</returns>
        Task<NetworkPeer> CreateConnectedNetworkPeerAsync(Network network, string endPoint, ProtocolVersion myVersion = ProtocolVersion.PROTOCOL_VERSION, bool isRelay = true, CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// Creates a new network peer which is connected to a specified counterparty.
        /// </summary>
        /// <param name="network">The network to connect to.</param>
        /// <param name="endPoint">Address and port of the counterparty to connect to.</param>
        /// <param name="parameters">Parameters specifying how the connection with the counterparty should be established, or <c>null</c> to use default parameters.</param>
        /// <returns>Network peer connected to the specified counterparty.</returns>
        Task<NetworkPeer> CreateConnectedNetworkPeerAsync(Network network, IPEndPoint endPoint, NetworkPeerConnectionParameters parameters = null);

        /// <summary>
        /// Creates a new network peer which is connected to a specified counterparty.
        /// </summary>
        /// <param name="network">The network to connect to.</param>
        /// <param name="peerAddress">Address and port of the counterparty to connect to.</param>
        /// <param name="parameters">Parameters specifying how the connection with the counterparty should be established, or <c>null</c> to use default parameters.</param>
        /// <returns>Network peer connected to the specified counterparty.</returns>
        Task<NetworkPeer> CreateConnectedNetworkPeerAsync(Network network, NetworkAddress peerAddress, NetworkPeerConnectionParameters parameters = null);

        /// <summary>
        /// Creates a new network peer server.
        /// <para>When created, the server is ready to be started, but this method does not start listening.</para>
        /// </summary>
        /// <param name="network">Specification of the network the node runs on - regtest/testnet/mainnet.</param>
        /// <param name="localEndPoint">IP address and port to listen on.</param>
        /// <param name="externalEndPoint">IP address and port that the server is reachable from the Internet on.</param>
        /// <param name="version">Version of the network protocol that the server should run.</param>
        /// <returns>Newly created network peer server, which is ready to be started.</returns>
        NetworkPeerServer CreateNetworkPeerServer(Network network, IPEndPoint localEndPoint, IPEndPoint externalEndPoint, ProtocolVersion version = ProtocolVersion.PROTOCOL_VERSION);


        /// <summary>
        /// Creates a new representation of the network connection using TCP client object.
        /// </summary>
        /// <param name="peer">Network peer the node is connected to, or will connect to.</param>
        /// <param name="client">Initialized and possibly connected TCP client to the peer.</param>
        /// <param name="messageReceivedCallback">Callback to be called when a new message arrives from the peer.</param>
        NetworkPeerConnection CreateNetworkPeerConnection(NetworkPeer peer, TcpClient client, Func<IncomingMessage, Task> messageReceivedCallback);
    }

    /// <summary>
    /// Factory for creating P2P network peers.
    /// </summary>
    public class NetworkPeerFactory : INetworkPeerFactory
    {
        /// <summary>Factory for creating loggers.</summary>
        private readonly ILoggerFactory loggerFactory;
        
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>Provider of time functions.</summary>
        private readonly IDateTimeProvider dateTimeProvider;

        /// <summary>Specification of the network the node runs on - regtest/testnet/mainnet.</summary>
        private readonly Network network;

        /// <summary>Identifier of the last network peer client this factory produced.</summary>
        /// <remarks>When a new client is created, the ID is incremented so that each client has its own unique ID.</remarks>
        private int lastClientId;

        /// <summary>
        /// Initializes a new instance of the factory.
        /// </summary>
        /// <param name="network">Specification of the network the node runs on - regtest/testnet/mainnet.</param>
        /// <param name="dateTimeProvider">Provider of time functions.</param>
        /// <param name="loggerFactory">Factory for creating loggers.</param>
        public NetworkPeerFactory(Network network, IDateTimeProvider dateTimeProvider, ILoggerFactory loggerFactory)
        {
            Guard.NotNull(network, nameof(network));
            Guard.NotNull(dateTimeProvider, nameof(dateTimeProvider));
            Guard.NotNull(loggerFactory, nameof(loggerFactory));

            this.network = network;
            this.dateTimeProvider = dateTimeProvider;
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.lastClientId = 0;
        }

        /// <inheritdoc/>
        public NetworkPeer CreateNetworkPeer(Network network, TcpClient client, NetworkPeerConnectionParameters parameters = null)
        {
            Guard.NotNull(network, nameof(network));
            Guard.NotNull(client, nameof(client));

            var peerAddress = new NetworkAddress()
            {
                Endpoint = (IPEndPoint)client.Client.RemoteEndPoint,
                Time = this.dateTimeProvider.GetUtcNow()
            };

            return new NetworkPeer(peerAddress, network, parameters, client, this.dateTimeProvider, this, this.loggerFactory);
        }

        /// <inheritdoc/>
        public async Task<NetworkPeer> CreateConnectedNetworkPeerAsync(Network network, string endPoint, ProtocolVersion myVersion = ProtocolVersion.PROTOCOL_VERSION, bool isRelay = true, CancellationToken cancellation = default(CancellationToken))
        {
            Guard.NotNull(network, nameof(network));
            Guard.NotNull(endPoint, nameof(endPoint));

            IPEndPoint ipEndPoint = Utils.ParseIpEndpoint(endPoint, network.DefaultPort);
            var parameters = new NetworkPeerConnectionParameters()
            {
                ConnectCancellation = cancellation,
                IsRelay = isRelay,
                Version = myVersion,
                Services = NetworkPeerServices.Nothing,
            };

            return await this.CreateConnectedNetworkPeerAsync(network, ipEndPoint, parameters);
        }

        /// <inheritdoc/>
        public async Task<NetworkPeer> CreateConnectedNetworkPeerAsync(Network network, IPEndPoint endPoint, NetworkPeerConnectionParameters parameters = null)
        {
            Guard.NotNull(network, nameof(network));
            Guard.NotNull(endPoint, nameof(endPoint));

            var peerAddress = new NetworkAddress()
            {
                Time = this.dateTimeProvider.GetTimeOffset(),
                Endpoint = endPoint
            };

            return await this.CreateConnectedNetworkPeerAsync(network, peerAddress, parameters);
        }

        /// <inheritdoc/>
        public async Task<NetworkPeer> CreateConnectedNetworkPeerAsync(Network network, NetworkAddress peerAddress, NetworkPeerConnectionParameters parameters = null)
        {
            Guard.NotNull(network, nameof(network));
            Guard.NotNull(peerAddress, nameof(peerAddress));

            var peer = new NetworkPeer(peerAddress, network, parameters, this, this.dateTimeProvider, this.loggerFactory);
            try
            {
                await peer.ConnectAsync(peer.Parameters.ConnectCancellation).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                peer.Dispose("Connection failed", e);
                throw;
            }
            return peer;
        }

        /// <inheritdoc/>
        public NetworkPeerServer CreateNetworkPeerServer(Network network, IPEndPoint localEndPoint, IPEndPoint externalEndPoint, ProtocolVersion version = ProtocolVersion.PROTOCOL_VERSION)
        {
            Guard.NotNull(network, nameof(network));
            Guard.NotNull(localEndPoint, nameof(localEndPoint));
            Guard.NotNull(externalEndPoint, nameof(externalEndPoint));

            return new NetworkPeerServer(network, localEndPoint, externalEndPoint, version, this.dateTimeProvider, this.loggerFactory, this);
        }

        /// <inheritdoc/>
        public NetworkPeerConnection CreateNetworkPeerConnection(NetworkPeer peer, TcpClient client, Func<IncomingMessage, Task> messageReceivedCallback)
        {
            Guard.NotNull(peer, nameof(peer));
            Guard.NotNull(client, nameof(client));
            Guard.NotNull(messageReceivedCallback, nameof(messageReceivedCallback));

            int id = Interlocked.Increment(ref this.lastClientId);
            return new NetworkPeerConnection(this.network, peer, client, id, messageReceivedCallback, this.dateTimeProvider, this.loggerFactory);
        }
    }
}