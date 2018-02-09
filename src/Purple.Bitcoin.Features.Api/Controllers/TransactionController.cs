using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using Purple.Bitcoin.Features.BlockStore;

namespace Purple.Bitcoin.Features.Api.Controllers
{
    [Route("api/tx")]
    public class TransactionController : Controller
    {
        private readonly IFullNode fullNode;

        private readonly IBlockRepository blockRepository;

        public TransactionController(IFullNode fullNode, IBlockRepository blockRepository)
        {
            this.fullNode = fullNode;
            this.blockRepository = blockRepository;
        }

        /// <summary>
        /// Returns the transaction with the required id.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("{txid}")]
        public async Task<IActionResult> BlockAsync(string txid)
        {
            var tx = await this.blockRepository.GetTrxAsync(new uint256(txid));
            return new JsonResult(tx);
        }
    }
}
