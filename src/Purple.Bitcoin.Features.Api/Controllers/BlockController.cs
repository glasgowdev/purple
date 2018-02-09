using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using Purple.Bitcoin.Base;
using Purple.Bitcoin.Features.BlockStore;

namespace Purple.Bitcoin.Features.Api.Controllers
{
    [Route("api/block")]
    public class BlockController : Controller
    {
        private readonly IFullNode fullNode;

        private readonly IChainRepository chainRepository;

        private readonly IBlockRepository blockRepository;

        public BlockController(IFullNode fullNode, IChainRepository chainRepository, IBlockRepository blockRepository)
        {
            this.fullNode = fullNode;
            this.chainRepository = chainRepository;
            this.blockRepository = blockRepository;
        }

        /// <summary>
        /// Returns the block at the specified height.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("{height:int}")]
        public async Task<IActionResult> BlockAsync(int height)
        {
            BlockHeader header = await this.chainRepository.GetAsync(height);
            uint256 hash = header.GetHash(this.fullNode.Network.NetworkOptions);
            Block block = await this.blockRepository.GetAsync(hash);
            return new JsonResult(block);
        }

        /// <summary>
        /// Returns the block with the required hash.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("{hash}")]
        public async Task<IActionResult> BlockAsync(string hash)
        {
            var block = await this.blockRepository.GetAsync(new uint256(hash));
            return new JsonResult(block);
        }
    }
}
