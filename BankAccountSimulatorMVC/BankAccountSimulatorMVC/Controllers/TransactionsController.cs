using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using BankAccountSimulatorMVC.ViewModels;
using BankAccountSimulatorMVC.Services;
using BankAccountSimulatorMVC.Services.Interface;

namespace BankAccountSimulatorMVC.Controllers;

public class TransactionsController(ITransactionService transactionService) : Controller
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

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

        var result = await transactionService.DepositAsync(model.AccountNumber, model.Amount);
        if (!result.Success) return ViewWithModelError(model, result.Message, "Deposit failed.");

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAccountDetails(model.AccountNumber);
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

        var result = await transactionService.WithdrawAsync(model.AccountNumber, model.Amount);
        if (!result.Success) return ViewWithModelError(model, result.Message, "Withdrawal failed.");

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAccountDetails(model.AccountNumber);
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

        var result = await transactionService.TransferAsync(model.SourceAccountNumber, model.DestinationAccountNumber, model.Amount);
        if (!result.Success) return ViewWithModelError(model, result.Message, "Transfer failed.");

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAccountDetails(model.SourceAccountNumber);
    }

    [HttpGet]
    public async Task<IActionResult> History(
        string? accountNumber,
        TransactionFilter filter = TransactionFilter.All,
        int page = 1,
        int pageSize = DefaultPageSize)
    {
        var filterType = filter switch
        {
            TransactionFilter.Deposits => TransactionType.Deposit,
            TransactionFilter.Withdrawals => TransactionType.Withdraw,
            TransactionFilter.Transfers => TransactionType.Transfer,
            _ => (TransactionType?)null
        };

        var normalizedPageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var transactions = await transactionService.GetHistoryAsync(accountNumber, filterType);
        var totalItems = transactions.Count;
        var totalPages = totalItems <= 0 ? 1 : (int)Math.Ceiling(totalItems / (double)normalizedPageSize);
        var normalizedPage = page <= 0 ? 1 : Math.Min(page, totalPages);

        var pagedTransactions = transactions
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToList();

        var viewModel = new TransactionHistoryViewModel
        {
            AccountNumber = accountNumber,
            Filter = filter,
            Transactions = pagedTransactions,
            CurrentPage = normalizedPage,
            PageSize = normalizedPageSize,
            TotalItems = totalItems
        };

        return View(viewModel);
    }

    private IActionResult RedirectToAccountDetails(string accountNumber)
    {
        return RedirectToAction("Details", "Accounts", new { accountNumber });
    }

    private IActionResult ViewWithModelError<TModel>(TModel model, string? message, string fallbackMessage)
    {
        ModelState.AddModelError(string.Empty, message ?? fallbackMessage);
        return View(model);
    }
}
