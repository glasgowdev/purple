using System.Collections.Generic;
using System.Linq;
using NBitcoin;
using Purple.Bitcoin.Utilities;

namespace Purple.Bitcoin.Features.Consensus
{
    /// <summary>
    /// Interface of block header hash checkpoint provider.
    /// </summary>
    public interface ICheckpoints
    {
        /// <summary>
        /// Obtains a height of the last checkpointed block.
        /// </summary>
        /// <returns>Height of the last checkpointed block, or 0 if no checkpoint is available.</returns>
        int GetLastCheckpointHeight();

        /// <summary>
        /// Checks if a block header hash at specific height is in violation with the hardcoded checkpoints.
        /// </summary>
        /// <param name="height">Height of the block.</param>
        /// <param name="hash">Block header hash to check.</param>
        /// <returns><c>true</c> if either there is no checkpoint for the given height, or if the checkpointed block header hash equals to the checked <paramref name="hash"/>.
        /// <c>false</c> if there is a checkpoint for the given <paramref name="height"/>, but the checkpointed block header hash is not the same as the checked <paramref name="hash"/>.</returns>
        bool CheckHardened(int height, uint256 hash);

        /// <summary>
        /// Retrieves checkpoint for a block at given height.
        /// </summary>
        /// <param name="height">Height of the block.</param>
        /// <returns>Checkpoint information or <c>null</c> if a checkpoint does not exist for given <paramref name="height"/>.</returns>
        CheckpointInfo GetCheckpoint(int height);
    }

    /// <summary>
    /// Description of checkpointed block.
    /// </summary>
    public class CheckpointInfo
    {
        /// <summary>Hash of the checkpointed block header.</summary>
        public uint256 Hash { get; set; }

        /// <summary>Stake modifier V2 value of the checkpointed block.</summary>
        /// <remarks>Purple only.</remarks>
        public uint256 StakeModifierV2 { get; set; }

        /// <summary>
        /// Initializes a new instance of the object.
        /// </summary>
        /// <param name="hash">Hash of the checkpointed block header.</param>
        /// <param name="stakeModifierV2">Stake modifier V2 value of the checkpointed block. Purple network only.</param>
        public CheckpointInfo(uint256 hash, uint256 stakeModifierV2 = null)
        {
            this.Hash = hash;
            this.StakeModifierV2 = stakeModifierV2;
        }
    }

    /// <summary>
    /// Checkpoints is a mechanism on how to avoid validation of historic blocks for which there
    /// already is a consensus on the network. This allows speeding up IBD, especially on POS networks.
    /// </summary>
    /// <remarks>
    /// From https://github.com/bitcoin/bitcoin/blob/b1973d6181eacfaaf45effb67e0c449ea3a436b8/src/chainparams.cpp#L66 :
    /// What makes a good checkpoint block? It is surrounded by blocks with reasonable timestamps
    //  (no blocks before with a timestamp after, none after with timestamp before). It also contains
    //  no strange transactions.
    /// </remarks>
    public class Checkpoints : ICheckpoints
    {
        /// <summary>List of selected checkpoints for PPL mainnet.</summary>
        private static Dictionary<int, CheckpointInfo> PurpleMainnetCheckpoints = new Dictionary<int, CheckpointInfo>
        {
        };

        /// <summary>List of selected checkpoints for PPL testnet.</summary>
        private static Dictionary<int, CheckpointInfo> PurpleTestnetCheckpoints = new Dictionary<int, CheckpointInfo>
        {
        };

        /// <summary>Checkpoints for the specific instance of the class and its network.</summary>
        private readonly Dictionary<int, CheckpointInfo> checkpoints;

        /// <summary>
        /// Initializes a new instance of the object.
        /// </summary>
        public Checkpoints()
        {
            this.checkpoints = new Dictionary<int, CheckpointInfo>();
        }

        /// <summary>
        /// Initializes a new instance of the object.
        /// </summary>
        /// <param name="network">Specification of the network the node runs on - regtest/testnet/mainnet/Purple test/main.</param>
        /// <param name="settings">Consensus settings for node - used to see if checkpoints have been disabled or not.</param>
        public Checkpoints(Network network, ConsensusSettings settings)
        {
            Guard.NotNull(network, nameof(network));
            Guard.NotNull(settings, nameof(settings));

            if (!settings.UseCheckpoints) this.checkpoints = new Dictionary<int, CheckpointInfo>();
            else if (network.Equals(Network.PurpleMain)) this.checkpoints = PurpleMainnetCheckpoints;
            else if (network.Equals(Network.PurpleTest)) this.checkpoints = PurpleTestnetCheckpoints;
            else if (network.Equals(Network.PurpleRegTest)) this.checkpoints = new Dictionary<int, CheckpointInfo>();
            else this.checkpoints = new Dictionary<int, CheckpointInfo>();
        }

        /// <inheritdoc />
        public int GetLastCheckpointHeight()
        {
            return this.checkpoints.Count > 0 ? this.checkpoints.Keys.Last() : 0;
        }

        /// <inheritdoc />
        public bool CheckHardened(int height, uint256 hash)
        {
            CheckpointInfo checkpoint;
            if (!this.checkpoints.TryGetValue(height, out checkpoint)) return true;

            return checkpoint.Hash.Equals(hash);
        }

        /// <inheritdoc />
        public CheckpointInfo GetCheckpoint(int height)
        {
            CheckpointInfo checkpoint;
            this.checkpoints.TryGetValue(height, out checkpoint);
            return checkpoint;
        }
    }
}
