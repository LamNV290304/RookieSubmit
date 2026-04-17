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
    public class AccountsControllerTests
    {
        private readonly Mock<IBankAccountService> _mockService;
        private readonly AccountsController _controller;

        public AccountsControllerTests()
        {
            _mockService = new Mock<IBankAccountService>();
            _controller = new AccountsController(_mockService.Object);

            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.TempData = tempData;
        }

        [Fact]
        public async Task Index_ShouldReturnViewModel_WithPagination()
        {
            // Arrange
            var accounts = Enumerable.Range(1000, 1011)
                .Select(i => new BankAccount { AccountNumber = $"A{i}" })
                .ToList();
            _mockService.Setup(x => x.GetAllAsync()).ReturnsAsync(accounts);

            // Act
            var result = await _controller.Index(page: 2, pageSize: 3);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AccountsIndexViewModel>(viewResult.Model);
            Assert.Equal(2, model.CurrentPage);
            Assert.Equal(3, model.PageSize);
            Assert.Equal(1011, model.TotalItems);
            Assert.Equal(3, model.Accounts.Count);
            Assert.Equal("A1003", model.Accounts[0].AccountNumber);
        }

        [Fact]
        public async Task Index_ShouldNormalizePageAndPageSize_WhenInvalidValues()
        {
            // Arrange
            var accounts = Enumerable.Range(1, 2)
                .Select(i => new BankAccount { AccountNumber = $"A{i}" })
                .ToList();
            _mockService.Setup(x => x.GetAllAsync()).ReturnsAsync(accounts);

            // Act
            var result = await _controller.Index(page: 0, pageSize: 0);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AccountsIndexViewModel>(viewResult.Model);
            Assert.Equal(1, model.CurrentPage);
            Assert.Equal(7, model.PageSize);
            Assert.Equal(2, model.TotalItems);
        }

        [Fact]
        public async Task Index_ShouldCapPageSizeAtMax100()
        {
            // Arrange
            var accounts = Enumerable.Range(1, 150)
                .Select(i => new BankAccount { AccountNumber = $"A{i}" })
                .ToList();
            _mockService.Setup(x => x.GetAllAsync()).ReturnsAsync(accounts);

            // Act
            var result = await _controller.Index(page: 1, pageSize: 1000);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AccountsIndexViewModel>(viewResult.Model);
            Assert.Equal(100, model.PageSize);
            Assert.Equal(100, model.Accounts.Count);
        }

        [Fact]
        public void Create_Get_ShouldReturnView_WithDefaultModel()
        {
            // Act
            var result = _controller.Create();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<CreateAccountViewModel>(viewResult.Model);
        }

        [Fact]
        public async Task Create_Post_ShouldReturnView_WhenModelStateInvalid()
        {
            // Arrange
            var model = new CreateAccountViewModel { AccountNumber = "A1001", OwnerName = "Test", InitialBalance = 10 };
            _controller.ModelState.AddModelError("AccountNumber", "required");

            // Act
            var result = await _controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            _mockService.Verify(x => x.CreateAsync(It.IsAny<BankAccount>()), Times.Never);
        }

        [Fact]
        public async Task Create_Post_ShouldReturnViewAndModelError_WhenServiceFails()
        {
            // Arrange
            var model = new CreateAccountViewModel { AccountNumber = "A1001", OwnerName = "Test", InitialBalance = 10 };
            _mockService.Setup(x => x.CreateAsync(It.IsAny<BankAccount>()))
                .ReturnsAsync(ServiceResult.Fail("duplicate"));

            // Act
            var result = await _controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState[string.Empty]!.Errors, e => e.ErrorMessage == "duplicate");
        }

        [Fact]
        public async Task Create_Post_ShouldRedirectToIndexAndSetTempData_WhenSuccess()
        {
            // Arrange
            var model = new CreateAccountViewModel { AccountNumber = " A1001 ", OwnerName = "Test", InitialBalance = 10 };
            _mockService.Setup(x => x.CreateAsync(It.IsAny<BankAccount>()))
                .ReturnsAsync(ServiceResult.Ok("created"));

            // Act
            var result = await _controller.Create(model);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("created", _controller.TempData["SuccessMessage"]);
            _mockService.Verify(x => x.CreateAsync(It.Is<BankAccount>(a =>
                a.AccountNumber == model.AccountNumber &&
                a.OwnerName == model.OwnerName &&
                a.Balance == model.InitialBalance)), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Details_ShouldReturnNotFound_WhenAccountNumberInvalid(string? accountNumber)
        {
            // Act
            var result = await _controller.Details(accountNumber!);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ShouldReturnNotFound_WhenAccountDoesNotExist()
        {
            // Arrange
            _mockService.Setup(x => x.GetByAccountNumberAsync("A1001")).ReturnsAsync((BankAccount?)null);

            // Act
            var result = await _controller.Details("A1001");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ShouldReturnView_WhenAccountExists()
        {
            // Arrange
            var account = new BankAccount { AccountNumber = "A1001" };
            _mockService.Setup(x => x.GetByAccountNumberAsync("A1001")).ReturnsAsync(account);

            // Act
            var result = await _controller.Details("A1001");
            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(account, view.Model);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Freeze_ShouldRedirectWithError_WhenAccountNumberInvalid(string? accountNumber)
        {
            // Act
            var result = await _controller.Freeze(accountNumber!, page: 2, pageSize: 8);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Account number is required.", _controller.TempData["ErrorMessage"]);
            Assert.Equal(2, redirect.RouteValues!["page"]);
            Assert.Equal(8, redirect.RouteValues["pageSize"]);
        }

        [Fact]
        public async Task Freeze_ShouldSetSuccessMessage_WhenServiceSuccess()
        {
            // Arrange
            _mockService.Setup(x => x.FreezeAsync("A1001")).ReturnsAsync(ServiceResult.Ok("frozen"));

            // Act
            var result = await _controller.Freeze("A1001", page: 1, pageSize: 7);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("frozen", _controller.TempData["SuccessMessage"]);
        }

        [Fact]
        public async Task Freeze_ShouldSetErrorMessage_WhenServiceFails()
        {
            // Arrange
            _mockService.Setup(x => x.FreezeAsync("A1001")).ReturnsAsync(ServiceResult.Fail("cannot freeze"));

            // Act
            var result = await _controller.Freeze("A1001", page: 1, pageSize: 7);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("cannot freeze", _controller.TempData["ErrorMessage"]);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Unfreeze_ShouldRedirectWithError_WhenAccountNumberInvalid(string? accountNumber)
        {
            // Act
            var result = await _controller.Unfreeze(accountNumber!, page: 3, pageSize: 9);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Account number is required.", _controller.TempData["ErrorMessage"]);
            Assert.Equal(3, redirect.RouteValues!["page"]);
            Assert.Equal(9, redirect.RouteValues["pageSize"]);
        }

        [Fact]
        public async Task Unfreeze_ShouldSetSuccessMessage_WhenServiceSuccess()
        {
            // Arrange
            _mockService.Setup(x => x.UnfreezeAsync("A1001")).ReturnsAsync(ServiceResult.Ok("unfrozen"));

            // Act
            var result = await _controller.Unfreeze("A1001", page: 1, pageSize: 7);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("unfrozen", _controller.TempData["SuccessMessage"]);
        }

        [Fact]
        public async Task Unfreeze_ShouldSetErrorMessage_WhenServiceFails()
        {
            // Arrange
            _mockService.Setup(x => x.UnfreezeAsync("A1001")).ReturnsAsync(ServiceResult.Fail("cannot unfreeze"));

            // Act
            var result = await _controller.Unfreeze("A1001", page: 1, pageSize: 7);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("cannot unfreeze", _controller.TempData["ErrorMessage"]);
        }
    }
}
