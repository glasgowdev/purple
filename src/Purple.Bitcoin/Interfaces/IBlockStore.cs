using System.Threading.Tasks;
using NBitcoin;

namespace Purple.Bitcoin.Interfaces
{
    public interface IBlockStore
    {
        Task<Transaction> GetTrxAsync(uint256 trxid);

        Task<uint256> GetTrxBlockIdAsync(uint256 trxid);
    }
}
