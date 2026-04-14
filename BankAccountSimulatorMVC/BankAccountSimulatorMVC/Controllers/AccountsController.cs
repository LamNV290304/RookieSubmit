using Domain.Models;
using BankAccountSimulatorMVC.Services;
using BankAccountSimulatorMVC.ViewModels;
using BankAccountSimulatorMVC.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace BankAccountSimulatorMVC.Controllers;

public class AccountsController(IBankAccountService bankAccountService) : Controller
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = DefaultPageSize)
    {
        var accounts = await bankAccountService.GetAllAsync();

        var normalizedPageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        var totalItems = accounts.Count;
        var totalPages = totalItems <= 0 ? 1 : (int)Math.Ceiling(totalItems / (double)normalizedPageSize);
        var normalizedPage = page <= 0 ? 1 : Math.Min(page, totalPages);

        var pagedAccounts = accounts
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToList();

        var viewModel = new AccountsIndexViewModel
        {
            Accounts = pagedAccounts,
            CurrentPage = normalizedPage,
            PageSize = normalizedPageSize,
            TotalItems = totalItems
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateAccountViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAccountViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var account = new BankAccount
        {
            AccountNumber = model.AccountNumber,
            OwnerName = model.OwnerName,
            Balance = model.InitialBalance
        };

        var result = await bankAccountService.CreateAsync(account);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Could not create account.");
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(string accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber)) return NotFound();

        var account = await bankAccountService.GetByAccountNumberAsync(accountNumber);
        if (account is null) return NotFound();

        return View(account);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Freeze(string accountNumber, int page = 1, int pageSize = DefaultPageSize)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            TempData["ErrorMessage"] = "Account number is required.";
            return RedirectToAction(nameof(Index), new { page, pageSize });
        }

        var result = await bankAccountService.FreezeAsync(accountNumber);
        return RedirectToIndexWithResult(result, page, pageSize);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unfreeze(string accountNumber, int page = 1, int pageSize = DefaultPageSize)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            TempData["ErrorMessage"] = "Account number is required.";
            return RedirectToAction(nameof(Index), new { page, pageSize });
        }

        var result = await bankAccountService.UnfreezeAsync(accountNumber);
        return RedirectToIndexWithResult(result, page, pageSize);
    }

    private IActionResult RedirectToIndexWithResult(ServiceResult result, int page = 1, int pageSize = DefaultPageSize)
    {
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index), new { page, pageSize });
    }
}
