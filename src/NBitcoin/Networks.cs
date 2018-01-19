using System;
using System.Diagnostics;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.DataEncoders;

namespace NBitcoin
{
    public partial class Network
    {
        static Network()
        {
            // initialize the networks
            bool saveTS = Transaction.TimeStamp;
            bool saveSig = Block.BlockSignature;
            Transaction.TimeStamp = false;
            Block.BlockSignature = false;

            Transaction.TimeStamp = saveTS;
            Block.BlockSignature = saveSig;
        }

        public static Network PurpleMain => Network.GetNetwork("PurpleMain") ?? InitPurpleMain();

        public static Network PurpleTest => Network.GetNetwork("PurpleTest") ?? InitPurpleTest();

        public static Network PurpleRegTest => Network.GetNetwork("PurpleRegTest") ?? InitPurpleRegTest();

        private static Network InitPurpleMain()
        {
            Block.BlockSignature = true;
            Transaction.TimeStamp = true;

            var consensus = new Consensus();

            consensus.NetworkOptions = new NetworkOptions() { IsProofOfStake = true };

            //consensus.GetPoWHash = (n, h) => Crypto.CryptoNight.Instance.Hash(h.ToBytes(options: n));
            consensus.GetPoWHash = (n, h) => Crypto.HashX15.Instance.Hash(h.ToBytes(options: n));

            consensus.SubsidyHalvingInterval = 262800;
            consensus.MajorityEnforceBlockUpgrade = 750;
            consensus.MajorityRejectBlockOutdated = 950;
            consensus.MajorityWindow = 1000;
            consensus.BuriedDeployments[BuriedDeployments.BIP34] = 227931;
            consensus.BuriedDeployments[BuriedDeployments.BIP65] = 388381;
            consensus.BuriedDeployments[BuriedDeployments.BIP66] = 363725;
            consensus.BIP34Hash = new uint256("0x00000456608ac183cb8084467363280cf09c1f43531f79569a92d2992929760b");
            consensus.PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"));
            consensus.PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60); // 1 day
            consensus.PowTargetSpacing = TimeSpan.FromSeconds(2 * 60); // 2 minutes
            consensus.PowAllowMinDifficultyBlocks = true;
            consensus.PowNoRetargeting = false;
            consensus.RuleChangeActivationThreshold = 684; // 95% of 720
            consensus.MinerConfirmationWindow = 720; // nPowTargetTimespan / nPowTargetSpacing

            consensus.BIP9Deployments[BIP9Deployments.TestDummy] = new BIP9DeploymentsParameters(28, 0, 0);
            consensus.BIP9Deployments[BIP9Deployments.CSV] = new BIP9DeploymentsParameters(0, 0, 0);
            consensus.BIP9Deployments[BIP9Deployments.Segwit] = new BIP9DeploymentsParameters(1, 0, 0);

            consensus.LastPOWBlock = 1314000;

            consensus.ProofOfStakeLimit = new BigInteger(uint256.Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false));
            consensus.ProofOfStakeLimitV2 = new BigInteger(uint256.Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false));

            consensus.CoinType = 174;

            consensus.DefaultAssumeValid = new uint256("0x8c2cf95f9ca72e13c8c4cdf15c2d7cc49993946fb49be4be147e106d502f1869"); // 642930

            Block genesis = CreateGenesisBlock(1515944103, 0, 0x1e0fffff, 1, Money.Zero);

            if (genesis.Header.Nonce == 0)
            {
                uint nonce = 0;
                uint last = 0;
                var watch = new Stopwatch();
                watch.Start();

                while (!genesis.CheckProofOfWork(consensus))
                {
                    genesis.Header.Nonce = ++nonce;
                    if (watch.ElapsedMilliseconds > 1000)
                    {
                        Console.WriteLine($"{nonce - last} H/s");
                        last = nonce;
                        watch.Restart();
                    }
                }

                watch.Stop();

                genesis.Header.CacheHashes();
            }

            consensus.HashGenesisBlock = genesis.GetHash(consensus.NetworkOptions);

            // The message start string is designed to be unlikely to occur in normal data.
            // The characters are rarely used upper ASCII, not valid as UTF-8, and produce
            // a large 4-byte int at any alignment.
            var messageStart = new byte[4];
            messageStart[0] = 0x11;
            messageStart[1] = 0x10;
            messageStart[2] = 0x19;
            messageStart[3] = 0x07;
            var magic = BitConverter.ToUInt32(messageStart, 0);

            Assert(consensus.HashGenesisBlock == uint256.Parse("0x"));
            Assert(genesis.Header.HashMerkleRoot == uint256.Parse("0x"));

            var builder = new NetworkBuilder()
                .SetName("PurpleMain")
                .SetConsensus(consensus)
                .SetMagic(magic)
                .SetGenesis(genesis)
                .SetPort(16178)
                .SetRPCPort(16174)
                .SetTxFees(10000, 60000, 10000)

                //.AddDNSSeeds(new[]
                //{
                //})

                .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { (51) })
                .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { (125) })
                .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { (63 + 128) })
                .SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_NO_EC, new byte[] { 0x01, 0x42 })
                .SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_EC, new byte[] { 0x01, 0x43 })
                .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { (0x04), (0x88), (0xB2), (0x1E) })
                .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { (0x04), (0x88), (0xAD), (0xE4) })
                .SetBase58Bytes(Base58Type.PASSPHRASE_CODE, new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 })
                .SetBase58Bytes(Base58Type.CONFIRMATION_CODE, new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A })
                .SetBase58Bytes(Base58Type.STEALTH_ADDRESS, new byte[] { 0x2a })
                .SetBase58Bytes(Base58Type.ASSET_ID, new byte[] { 23 })
                .SetBase58Bytes(Base58Type.COLORED_ADDRESS, new byte[] { 0x13 })
                .SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, "bc")
                .SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, "bc");

            //var seed = new[] { "" };
            //var fixedSeeds = new List<NetworkAddress>();
            //// Convert the pnSeeds array into usable address objects.
            //Random rand = new Random();
            //TimeSpan oneWeek = TimeSpan.FromDays(7);
            //for (int i = 0; i < seed.Length; i++)
            //{
            //    // It'll only connect to one or two seed nodes because once it connects,
            //    // it'll get a pile of addresses with newer timestamps.                
            //    NetworkAddress addr = new NetworkAddress();
            //    // Seed nodes are given a random 'last seen time' of between one and two
            //    // weeks ago.
            //    addr.Time = DateTime.UtcNow - (TimeSpan.FromSeconds(rand.NextDouble() * oneWeek.TotalSeconds)) - oneWeek;
            //    addr.Endpoint = Utils.ParseIpEndpoint(seed[i], builder.Port);
            //    fixedSeeds.Add(addr);
            //}

            //builder.AddSeeds(fixedSeeds);
            return builder.BuildAndRegister();
        }

        private static Network InitPurpleTest()
        {
            Block.BlockSignature = true;
            Transaction.TimeStamp = true;

            var consensus = Network.PurpleMain.Consensus.Clone();
            consensus.PowLimit = new Target(uint256.Parse("0000ffff00000000000000000000000000000000000000000000000000000000"));

            // The message start string is designed to be unlikely to occur in normal data.
            // The characters are rarely used upper ASCII, not valid as UTF-8, and produce
            // a large 4-byte int at any alignment.
            var messageStart = new byte[4];
            messageStart[0] = 0x16;
            messageStart[1] = 0x24;
            messageStart[2] = 0x43;
            messageStart[3] = 0x04;
            var magic = BitConverter.ToUInt32(messageStart, 0); //0x5223570; 

            var genesis = Network.PurpleMain.GetGenesis();
            genesis.Header.Time = 1515944095;
            genesis.Header.Nonce = 0;
            genesis.Header.Bits = consensus.PowLimit;

            consensus.HashGenesisBlock = genesis.GetHash(consensus.NetworkOptions);

            Assert(consensus.HashGenesisBlock == uint256.Parse("0x"));

            consensus.DefaultAssumeValid = new uint256("0x12ae16993ce7f0836678f225b2f4b38154fa923bd1888f7490051ddaf4e9b7fa"); // 218810

            var builder = new NetworkBuilder()
                .SetName("PurpleTest")
                .SetConsensus(consensus)
                .SetMagic(magic)
                .SetGenesis(genesis)
                .SetPort(26178)
                .SetRPCPort(26174)
                .SetTxFees(10000, 60000, 10000)
                .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { (65) })
                .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { (196) })
                .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { (65 + 128) })
                .SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_NO_EC, new byte[] { 0x01, 0x42 })
                .SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_EC, new byte[] { 0x01, 0x43 })
                .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { (0x04), (0x88), (0xB2), (0x1E) })
                .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { (0x04), (0x88), (0xAD), (0xE4) });

            //.AddDNSSeeds(new[]
            //{
            //});

            //builder.AddSeeds(new[] { new NetworkAddress(IPAddress.Parse(""), builder.Port) }); // the c# testnet node

            return builder.BuildAndRegister();
        }

        private static Network InitPurpleRegTest()
        {
            // TODO: move this to Networks
            var net = Network.GetNetwork("PurpleRegTest");
            if (net != null)
                return net;

            Block.BlockSignature = true;
            Transaction.TimeStamp = true;

            var consensus = Network.PurpleTest.Consensus.Clone();
            consensus.PowLimit = new Target(uint256.Parse("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"));

            consensus.PowAllowMinDifficultyBlocks = true;
            consensus.PowNoRetargeting = true;

            var messageStart = new byte[4];
            messageStart[0] = 0xdd;
            messageStart[1] = 0xd6;
            messageStart[2] = 0xea;
            messageStart[3] = 0xf7;
            var magic = BitConverter.ToUInt32(messageStart, 0);

            var genesis = Network.PurpleMain.GetGenesis();
            genesis.Header.Time = 1515944354;
            genesis.Header.Nonce = 0;
            genesis.Header.Bits = consensus.PowLimit;

            consensus.HashGenesisBlock = genesis.GetHash(consensus.NetworkOptions);

            Assert(consensus.HashGenesisBlock == uint256.Parse("0x"));

            consensus.DefaultAssumeValid = null; // turn off assumevalid for regtest.

            var builder = new NetworkBuilder()
                .SetName("PurpleRegTest")
                .SetConsensus(consensus)
                .SetMagic(magic)
                .SetGenesis(genesis)
                .SetPort(18444)
                .SetRPCPort(18442)
                .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { (65) })
                .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { (196) })
                .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { (65 + 128) })
                .SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_NO_EC, new byte[] { 0x01, 0x42 })
                .SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_EC, new byte[] { 0x01, 0x43 })
                .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { (0x04), (0x88), (0xB2), (0x1E) })
                .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { (0x04), (0x88), (0xAD), (0xE4) });

            return builder.BuildAndRegister();
        }

        private static Block CreateGenesisBlock(uint nTime, uint nNonce, uint nBits, int nVersion, Money genesisReward)
        {
            string pszTimestamp = "Hawaii in shock after false missile alert";
            return CreateGenesisBlock(pszTimestamp, nTime, nNonce, nBits, nVersion, genesisReward);
        }

        private static Block CreateGenesisBlock(string pszTimestamp, uint nTime, uint nNonce, uint nBits, int nVersion, Money genesisReward)
        {
            Transaction txNew = new Transaction();
            txNew.Version = 1;
            txNew.Time = nTime;

            txNew.AddInput(new TxIn()
            {
                ScriptSig = new Script(Op.GetPushOp(0), new Op()
                {
                    Code = (OpcodeType)0x1,
                    PushData = new[] { (byte)42 }
                }, Op.GetPushOp(Encoders.ASCII.DecodeData(pszTimestamp)))
            });

            txNew.AddOutput(new TxOut()
            {
                Value = genesisReward,
            });

            Block genesis = new Block();
            genesis.Header.BlockTime = Utils.UnixTimeToDateTime(nTime);
            genesis.Header.Bits = nBits;
            genesis.Header.Nonce = nNonce;
            genesis.Header.Version = nVersion;
            genesis.Transactions.Add(txNew);
            genesis.Header.HashPrevBlock = uint256.Zero;
            genesis.UpdateMerkleRoot();
            return genesis;
        }
    }
}
