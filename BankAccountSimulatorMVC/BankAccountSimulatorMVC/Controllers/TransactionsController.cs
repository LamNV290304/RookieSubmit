using BankAccountSimulatorMVC.Models;
using BankAccountSimulatorMVC.Models.ViewModels;
using BankAccountSimulatorMVC.Services;
using BankAccountSimulatorMVC.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace BankAccountSimulatorMVC.Controllers
{
	public class TransactionsController : Controller
	{
		private readonly ITransactionService _transactionService;

		public TransactionsController(ITransactionService transactionService)
		{
			_transactionService = transactionService;
		}

		[HttpGet]
		public IActionResult Deposit(string? accountNumber)
		{
			return View(new DepositWithdrawViewModel
			{
				AccountNumber = accountNumber ?? string.Empty
			});
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Deposit(DepositWithdrawViewModel model)
		{
			if (!ModelState.IsValid) return View(model);

			try
			{
				var result = await _transactionService.DepositAsync(model.AccountNumber, model.Amount);
				if (!result.Success)
				{
					ModelState.AddModelError(string.Empty, result.Message ?? "Deposit failed.");
					return View(model);
				}

				TempData["SuccessMessage"] = result.Message;
				return RedirectToAction("Details", "Accounts", new { accountNumber = model.AccountNumber });
			}
			catch (DataStoreException)
			{
				ModelState.AddModelError(string.Empty, "Cannot process deposit right now. Please try again later.");
				return View(model);
			}
		}

		[HttpGet]
		public IActionResult Withdraw(string? accountNumber)
		{
			return View(new DepositWithdrawViewModel
			{
				AccountNumber = accountNumber ?? string.Empty
			});
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Withdraw(DepositWithdrawViewModel model)
		{
			if (!ModelState.IsValid) return View(model);

			try
			{
				var result = await _transactionService.WithdrawAsync(model.AccountNumber, model.Amount);
				if (!result.Success)
				{
					ModelState.AddModelError(string.Empty, result.Message ?? "Withdrawal failed.");
					return View(model);
				}

				TempData["SuccessMessage"] = result.Message;
				return RedirectToAction("Details", "Accounts", new { accountNumber = model.AccountNumber });
			}
			catch (DataStoreException)
			{
				ModelState.AddModelError(string.Empty, "Cannot process withdrawal right now. Please try again later.");
				return View(model);
			}
		}

		[HttpGet]
		public IActionResult Transfer(string? sourceAccountNumber)
		{
			return View(new TransferViewModel
			{
				SourceAccountNumber = sourceAccountNumber ?? string.Empty
			});
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Transfer(TransferViewModel model)
		{
			if (!ModelState.IsValid) return View(model);

			try
			{
				var result = await _transactionService.TransferAsync(
					model.SourceAccountNumber,
					model.DestinationAccountNumber,
					model.Amount);

				if (!result.Success)
				{
					ModelState.AddModelError(string.Empty, result.Message ?? "Transfer failed.");
					return View(model);
				}

				TempData["SuccessMessage"] = result.Message;
				return RedirectToAction("Details", "Accounts", new { accountNumber = model.SourceAccountNumber });
			}
			catch (DataStoreException)
			{
				ModelState.AddModelError(string.Empty, "Cannot process transfer right now. Please try again later.");
				return View(model);
			}
		}

		[HttpGet]
		public async Task<IActionResult> History(string? accountNumber, TransactionFilter filter = TransactionFilter.All)
		{
			try
			{
				var filterType = filter switch
				{
					TransactionFilter.Deposits => TransactionType.Deposit,
					TransactionFilter.Withdrawals => TransactionType.Withdraw,
					TransactionFilter.Transfers => TransactionType.Transfer,
					_ => (TransactionType?)null
				};

				var transactions = await _transactionService.GetHistoryAsync(accountNumber, filterType);
				var viewModel = new TransactionHistoryViewModel
				{
					AccountNumber = accountNumber,
					Filter = filter,
					Transactions = transactions
				};

				return View(viewModel);
			}
			catch (DataStoreException)
			{
				TempData["ErrorMessage"] = "Cannot read transaction history right now. Please try again later.";
				return View(new TransactionHistoryViewModel
				{
					AccountNumber = accountNumber,
					Filter = filter,
					Transactions = new List<Transaction>()
				});
			}
		}
	}
}
