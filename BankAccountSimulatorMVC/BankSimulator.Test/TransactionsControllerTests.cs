using Moq;
using Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using BankAccountSimulatorMVC.Services;
using BankAccountSimulatorMVC.ViewModels;
using BankAccountSimulatorMVC.Controllers;
using BankAccountSimulatorMVC.Services.Interface;

namespace BankSimulator.Test
{
    public class TransactionsControllerTests
    {
        private readonly Mock<ITransactionService> _mockService;
        private readonly TransactionsController _controller;

        public TransactionsControllerTests()
        {
            _mockService = new Mock<ITransactionService>();
            _controller = new TransactionsController(_mockService.Object);

            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.TempData = tempData;
        }

        [Fact]
        public void Deposit_Get_ShouldReturnView_WithAccountNumber()
        {
            // Act
            var result = _controller.Deposit("A1001");

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DepositWithdrawViewModel>(view.Model);
            Assert.Equal("A1001", model.AccountNumber);
        }

        [Fact]
        public async Task Deposit_Post_ShouldReturnView_WhenModelStateInvalid()
        {
            // Arrange
            var model = new DepositWithdrawViewModel { AccountNumber = "A1001", Amount = 100 };
            _controller.ModelState.AddModelError("Amount", "invalid");

            // Act
            var result = await _controller.Deposit(model);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(model, view.Model);
            _mockService.Verify(x => x.DepositAsync(It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
        }

        [Fact]
        public async Task Deposit_Post_ShouldReturnViewAndModelError_WhenServiceFails()
        {
            // Arrange
            var model = new DepositWithdrawViewModel { AccountNumber = "A1001", Amount = 100 };
            _mockService.Setup(x => x.DepositAsync("A1001", 100)).ReturnsAsync(ServiceResult.Fail("deposit error"));

            // Act
            var result = await _controller.Deposit(model);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(model, view.Model);
            Assert.Contains(_controller.ModelState[string.Empty]!.Errors, e => e.ErrorMessage == "deposit error");
        }

        [Fact]
        public async Task Deposit_Post_ShouldRedirectToAccountDetails_WhenSuccess()
        {
            // Arrange
            var model = new DepositWithdrawViewModel { AccountNumber = "A1001", Amount = 100 };
            _mockService.Setup(x => x.DepositAsync("A1001", 100)).ReturnsAsync(ServiceResult.Ok("ok"));

            // Act
            var result = await _controller.Deposit(model);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.Equal("Accounts", redirect.ControllerName);
            Assert.Equal("A1001", redirect.RouteValues!["accountNumber"]);
            Assert.Equal("ok", _controller.TempData["SuccessMessage"]);
        }

        [Fact]
        public void Withdraw_Get_ShouldReturnView_WithAccountNumber()
        {
            // Act
            var result = _controller.Withdraw("A1001");

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DepositWithdrawViewModel>(view.Model);
            Assert.Equal("A1001", model.AccountNumber);
        }

        [Fact]
        public async Task Withdraw_Post_ShouldReturnView_WhenModelStateInvalid()
        {
            // Arrange
            var model = new DepositWithdrawViewModel { AccountNumber = "A1001", Amount = 100 };
            _controller.ModelState.AddModelError("Amount", "invalid");

            // Act
            var result = await _controller.Withdraw(model);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(model, view.Model);
            _mockService.Verify(x => x.WithdrawAsync(It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
        }

        [Fact]
        public async Task Withdraw_Post_ShouldReturnViewAndModelError_WhenServiceFails()
        {
            // Arrange
            var model = new DepositWithdrawViewModel { AccountNumber = "A1001", Amount = 100 };
            _mockService.Setup(x => x.WithdrawAsync("A1001", 100)).ReturnsAsync(ServiceResult.Fail("withdraw error"));

            // Act
            var result = await _controller.Withdraw(model);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(model, view.Model);
            Assert.Contains(_controller.ModelState[string.Empty]!.Errors, e => e.ErrorMessage == "withdraw error");
        }

        [Fact]
        public async Task Withdraw_Post_ShouldRedirectToAccountDetails_WhenSuccess()
        {
            // Arrange
            var model = new DepositWithdrawViewModel { AccountNumber = "A1001", Amount = 100 };
            _mockService.Setup(x => x.WithdrawAsync("A1001", 100)).ReturnsAsync(ServiceResult.Ok("ok"));

            // Act
            var result = await _controller.Withdraw(model);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.Equal("Accounts", redirect.ControllerName);
            Assert.Equal("A1001", redirect.RouteValues!["accountNumber"]);
            Assert.Equal("ok", _controller.TempData["SuccessMessage"]);
        }

        [Fact]
        public void Transfer_Get_ShouldReturnView_WithSourceAccountNumber()
        {
            // Act
            var result = _controller.Transfer("A1001");

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<TransferViewModel>(view.Model);
            Assert.Equal("A1001", model.SourceAccountNumber);
        }

        [Fact]
        public async Task Transfer_Post_ShouldReturnView_WhenModelStateInvalid()
        {
            // Arrange
            var model = new TransferViewModel { SourceAccountNumber = "A1001", DestinationAccountNumber = "A1002", Amount = 100 };
            _controller.ModelState.AddModelError("Amount", "invalid");

            // Act
            var result = await _controller.Transfer(model);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(model, view.Model);
            _mockService.Verify(x => x.TransferAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
        }

        [Fact]
        public async Task Transfer_Post_ShouldReturnViewAndModelError_WhenServiceFails()
        {
            // Arrange
            var model = new TransferViewModel { SourceAccountNumber = "A1001", DestinationAccountNumber = "A1002", Amount = 100 };
            _mockService.Setup(x => x.TransferAsync("A1001", "A1002", 100)).ReturnsAsync(ServiceResult.Fail("transfer error"));

            // Act
            var result = await _controller.Transfer(model);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(model, view.Model);
            Assert.Contains(_controller.ModelState[string.Empty]!.Errors, e => e.ErrorMessage == "transfer error");
        }

        [Fact]
        public async Task Transfer_Post_ShouldRedirectToAccountDetails_WhenSuccess()
        {
            // Arrange
            var model = new TransferViewModel { SourceAccountNumber = "A1001", DestinationAccountNumber = "A1002", Amount = 100 };
            _mockService.Setup(x => x.TransferAsync("A1001", "A1002", 100)).ReturnsAsync(ServiceResult.Ok("ok"));

            // Act
            var result = await _controller.Transfer(model);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.Equal("Accounts", redirect.ControllerName);
            Assert.Equal("A1001", redirect.RouteValues!["accountNumber"]);
            Assert.Equal("ok", _controller.TempData["SuccessMessage"]);
        }

        [Fact]
        public async Task History_ShouldMapFilterAndReturnPagedViewModel()
        {
            // Arrange
            var transactions = Enumerable.Range(1, 25)
                .Select(i => new Transaction { AccountNumber = "A1001", Type = TransactionType.Withdraw, Amount = i })
                .ToList();
            _mockService.Setup(x => x.GetHistoryAsync("A1001", TransactionType.Withdraw)).ReturnsAsync(transactions);

            // Act
            var result = await _controller.History("A1001", TransactionFilter.Withdrawals, page: 2, pageSize: 10);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<TransactionHistoryViewModel>(view.Model);
            Assert.Equal("A1001", model.AccountNumber);
            Assert.Equal(TransactionFilter.Withdrawals, model.Filter);
            Assert.Equal(2, model.CurrentPage);
            Assert.Equal(10, model.PageSize);
            Assert.Equal(25, model.TotalItems);
            Assert.Equal(10, model.Transactions.Count);
            Assert.Equal(11, model.Transactions[0].Amount);
        }

        [Fact]
        public async Task History_ShouldUseDefaultFilter_WhenAll()
        {
            // Arrange
            _mockService.Setup(x => x.GetHistoryAsync("A1001", null)).ReturnsAsync([]);

            // Act
            var result = await _controller.History("A1001", TransactionFilter.All, page: 1, pageSize: 10);

            // Assert
            Assert.IsType<ViewResult>(result);
            _mockService.Verify(x => x.GetHistoryAsync("A1001", null), Times.Once);
        }

        [Fact]
        public async Task History_ShouldNormalizePageAndPageSize_WhenInvalidValues()
        {
            // Arrange
            _mockService.Setup(x => x.GetHistoryAsync(null, null)).ReturnsAsync([]);

            // Act
            var result = await _controller.History(null, TransactionFilter.All, page: 0, pageSize: 0);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<TransactionHistoryViewModel>(view.Model);
            Assert.Equal(1, model.CurrentPage);
            Assert.Equal(10, model.PageSize);
            Assert.Equal(0, model.TotalItems);
        }

        [Fact]
        public async Task History_ShouldCapPageSizeAtMax100()
        {
            // Arrange
            var transactions = Enumerable.Range(1, 150)
                .Select(i => new Transaction { AccountNumber = "A1001", Amount = i })
                .ToList();
            _mockService.Setup(x => x.GetHistoryAsync("A1001", null)).ReturnsAsync(transactions);

            // Act
            var result = await _controller.History("A1001", TransactionFilter.All, page: 1, pageSize: 1000);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<TransactionHistoryViewModel>(view.Model);
            Assert.Equal(100, model.PageSize);
            Assert.Equal(100, model.Transactions.Count);
        }
    }
}
