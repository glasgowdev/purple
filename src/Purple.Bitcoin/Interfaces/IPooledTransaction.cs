using System.Threading.Tasks;
using NBitcoin;

namespace Purple.Bitcoin.Interfaces
{
    public interface IPooledTransaction
    {
        Task<Transaction> GetTransaction(uint256 trxid);
    }
}
