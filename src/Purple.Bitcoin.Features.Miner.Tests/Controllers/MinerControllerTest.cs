﻿using System;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NBitcoin;
using Newtonsoft.Json;
using Purple.Bitcoin.Features.Miner.Controllers;
using Purple.Bitcoin.Features.Miner.Interfaces;
using Purple.Bitcoin.Features.Miner.Models;
using Purple.Bitcoin.Features.Wallet;
using Purple.Bitcoin.Features.Wallet.Interfaces;
using Purple.Bitcoin.Features.Wallet.Tests;
using Purple.Bitcoin.Tests.Logging;
using Purple.Bitcoin.Utilities.JsonErrors;
using Xunit;

namespace Purple.Bitcoin.Features.Miner.Tests.Controllers
{
    public class MinerControllerTest : LogsTestBase
    {
        private MinerController controller;
        private Mock<IFullNode> fullNode;
        private Mock<IPosMinting> posMinting;
        private Mock<IWalletManager> walletManager;

        public MinerControllerTest()
        {
            this.fullNode = new Mock<IFullNode>();
            this.posMinting = new Mock<IPosMinting>();
            this.walletManager = new Mock<IWalletManager>();

            this.controller = new MinerController(this.fullNode.Object, this.LoggerFactory.Object, this.walletManager.Object, this.posMinting.Object);
        }

        [Fact]
        public void GetStakingInfo_WithoutPosMinting_ReturnsEmptyStakingInfoModel()
        {
            this.controller = new MinerController(this.fullNode.Object, this.LoggerFactory.Object, null);

            var response = this.controller.GetStakingInfo();

            var jsonResult = Assert.IsType<JsonResult>(response);
            var result = Assert.IsType<GetStakingInfoModel>(jsonResult.Value);
            Assert.Equal(JsonConvert.SerializeObject(new GetStakingInfoModel()), JsonConvert.SerializeObject(result));
        }

        [Fact]
        public void GetStakingInfo_WithPosMinting_ReturnsPosMintingStakingInfoModel()
        {
            this.posMinting.Setup(p => p.GetGetStakingInfoModel())
                .Returns(new GetStakingInfoModel()
                {
                    Enabled = true,
                    CurrentBlockSize = 150000
                }).Verifiable();

            var response = this.controller.GetStakingInfo();

            var jsonResult = Assert.IsType<JsonResult>(response);
            var result = Assert.IsType<GetStakingInfoModel>(jsonResult.Value);
            Assert.True(result.Enabled);
            Assert.Equal(150000, result.CurrentBlockSize);
            this.posMinting.Verify();
        }

        [Fact]
        public void GetStakingInfo_UnexpectedException_ReturnsBadRequest()
        {
            this.posMinting.Setup(p => p.GetGetStakingInfoModel())
              .Throws(new InvalidOperationException("Unable to get model"));

            var result = this.controller.GetStakingInfo();

            ErrorResult errorResult = Assert.IsType<ErrorResult>(result);
            ErrorResponse errorResponse = Assert.IsType<ErrorResponse>(errorResult.Value);
            Assert.Single(errorResponse.Errors);

            ErrorModel error = errorResponse.Errors[0];
            Assert.Equal(400, error.Status);
            Assert.Equal("Unable to get model", error.Message);
        }

        [Fact]
        public void StartStaking_InvalidModelState_ReturnsBadRequest()
        {
            this.controller.ModelState.AddModelError("Password", "A password is required.");

            var result = this.controller.StartStaking(new StartStakingRequest());

            ErrorResult errorResult = Assert.IsType<ErrorResult>(result);
            ErrorResponse errorResponse = Assert.IsType<ErrorResponse>(errorResult.Value);
            Assert.Single(errorResponse.Errors);

            ErrorModel error = errorResponse.Errors[0];
            Assert.Equal(400, error.Status);
            Assert.Equal("Formatting error", error.Message);
        }

        [Fact]
        public void StartStaking_WalletNotFound_ReturnsBadRequest()
        {
            this.walletManager.Setup(w => w.GetWallet("myWallet"))
                .Throws(new WalletException("Wallet not found."));

            this.fullNode.Setup(f => f.NodeService<IWalletManager>(false))
                .Returns(this.walletManager.Object);

            var result = this.controller.StartStaking(new StartStakingRequest() { Name = "myWallet" });

            ErrorResult errorResult = Assert.IsType<ErrorResult>(result);
            ErrorResponse errorResponse = Assert.IsType<ErrorResponse>(errorResult.Value);
            Assert.Single(errorResponse.Errors);

            ErrorModel error = errorResponse.Errors[0];
            Assert.Equal(400, error.Status);
            Assert.Equal("Wallet not found.", error.Message);
        }

        [Fact]
        public void StartStaking_InvalidWalletPassword_ReturnsBadRequest()
        {
            var wallet = WalletTestsHelpers.GenerateBlankWallet("myWallet", "password1");
            this.walletManager.Setup(w => w.GetWallet("myWallet"))
              .Returns(wallet);

            this.fullNode.Setup(f => f.NodeService<IWalletManager>(false))
                .Returns(this.walletManager.Object);

            var result = this.controller.StartStaking(new StartStakingRequest() { Name = "myWallet", Password = "password2" });

            ErrorResult errorResult = Assert.IsType<ErrorResult>(result);
            ErrorResponse errorResponse = Assert.IsType<ErrorResponse>(errorResult.Value);
            Assert.Single(errorResponse.Errors);

            ErrorModel error = errorResponse.Errors[0];
            Assert.Equal(400, error.Status);
            Assert.Equal("Invalid password (or invalid Network)", error.Message);
        }

        [Fact]
        public void StartStaking_UnexpectedException_ReturnsBadRequest()
        {
            this.walletManager.Setup(w => w.GetWallet("myWallet"))
                   .Throws(new InvalidOperationException("Unable to get wallet"));

            this.fullNode.Setup(f => f.NodeService<IWalletManager>(false))
                .Returns(this.walletManager.Object);

            var result = this.controller.StartStaking(new StartStakingRequest() { Name = "myWallet" });

            ErrorResult errorResult = Assert.IsType<ErrorResult>(result);
            ErrorResponse errorResponse = Assert.IsType<ErrorResponse>(errorResult.Value);
            Assert.Single(errorResponse.Errors);

            ErrorModel error = errorResponse.Errors[0];
            Assert.Equal(400, error.Status);
            Assert.Equal("Unable to get wallet", error.Message);
        }

        [Fact]
        public void StartStaking_ValidWalletAndPassword_StartsStaking_ReturnsOk()
        {
            var wallet = WalletTestsHelpers.GenerateBlankWallet("myWallet", "password1");
            this.walletManager.Setup(w => w.GetWallet("myWallet"))
              .Returns(wallet);

            this.fullNode.Setup(f => f.NodeService<IWalletManager>(false))
                .Returns(this.walletManager.Object);

            this.fullNode.Setup(f => f.NodeFeature<MiningFeature>(true))
                .Returns(new MiningFeature(Network.PurpleMain, new MinerSettings(), Configuration.NodeSettings.Default(), this.LoggerFactory.Object, null, this.posMinting.Object, this.walletManager.Object));

            var result = this.controller.StartStaking(new StartStakingRequest() { Name = "myWallet", Password = "password1" });

            Assert.IsType<OkResult>(result);
            this.posMinting.Verify(p => p.Stake(It.Is<PosMinting.WalletSecret>(s => s.WalletName == "myWallet" && s.WalletPassword == "password1")), Times.Exactly(1));
        }
    }
}