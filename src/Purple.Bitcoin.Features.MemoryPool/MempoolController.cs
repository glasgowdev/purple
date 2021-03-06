﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using Purple.Bitcoin.Utilities;

namespace Purple.Bitcoin.Features.MemoryPool
{
    public class MempoolController : FeatureController
    {
        public MempoolManager MempoolManager { get; private set; }

        public MempoolController(MempoolManager mempoolManager)
        {
            Guard.NotNull(mempoolManager, nameof(mempoolManager));

            this.MempoolManager = mempoolManager;
        }

        [ActionName("getrawmempool")]
        [ActionDescription("Lists the contents of the memory pool.")]
        public Task<List<uint256>> GetRawMempool()
        {
            return this.MempoolManager.GetMempoolAsync();
        }
    }
}
