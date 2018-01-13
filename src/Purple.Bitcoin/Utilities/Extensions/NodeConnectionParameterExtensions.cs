using Purple.Bitcoin.P2P;
using Purple.Bitcoin.P2P.Peer;

namespace Purple.Bitcoin.Utilities.Extensions
{
    public static class NodeConnectionParameterExtensions
    {
        public static PeerAddressManagerBehaviour PeerAddressManagerBehaviour(this NetworkPeerConnectionParameters parameters)
        {
            return parameters.TemplateBehaviors.Find<PeerAddressManagerBehaviour>();
        }
    }
}