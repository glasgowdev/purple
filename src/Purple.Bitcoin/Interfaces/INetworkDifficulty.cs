using NBitcoin;

namespace Purple.Bitcoin.Interfaces
{
    public interface INetworkDifficulty
    {
        Target GetNetworkDifficulty();
    }
}
