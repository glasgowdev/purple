﻿using System;
using Purple.Bitcoin.P2P.Peer;
using Purple.Bitcoin.P2P.Protocol.Payloads;

namespace Purple.Bitcoin.P2P.Protocol.Filters
{
    /// <summary>
    /// Contract to intercept sent and received messages.
    /// </summary>
    public interface INetworkPeerFilter
    {
        /// <summary>
        /// Intercept a message before it can be processed by listeners
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="next">The rest of the pipeline</param>
        void OnReceivingMessage(IncomingMessage message, Action next);

        /// <summary>
        /// Intercept a message before it is sent to the peer
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="payload"></param>
        /// <param name="next">The rest of the pipeline</param>
        void OnSendingMessage(NetworkPeer peer, Payload payload, Action next);
    }
}
