using System;
using NBitcoin;
using Purple.Bitcoin.P2P.Peer;
using Purple.Bitcoin.P2P.Protocol.Payloads;

namespace Purple.Bitcoin.P2P.Protocol.Filters
{
    public class NetworkPeerFiltersCollection : ThreadSafeCollection<INetworkPeerFilter>
    {
        public IDisposable Add(Action<IncomingMessage, Action> onReceiving, Action<NetworkPeer, Payload, Action> onSending = null)
        {
            return base.Add(new ActionFilter(onReceiving, onSending));
        }
    }
}
