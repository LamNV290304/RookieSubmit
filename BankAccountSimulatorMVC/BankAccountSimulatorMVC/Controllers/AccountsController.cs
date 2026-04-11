using BankAccountSimulatorMVC.Models;
using BankAccountSimulatorMVC.Models.ViewModels;
using BankAccountSimulatorMVC.Services;
using BankAccountSimulatorMVC.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace BankAccountSimulatorMVC.Controllers
{
    public class AccountsController : Controller
    {
        private readonly IBankAccountService _bankAccountService;

        public AccountsController(IBankAccountService bankAccountService)
        {
            _bankAccountService = bankAccountService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var accounts = await _bankAccountService.GetAllAsync();
                return View(accounts);
            }
            catch (DataStoreException)
            {
                TempData["ErrorMessage"] = "Cannot read account data right now. Please try again later.";
                return View(new List<BankAccount>());
            }
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

            try
            {
                var account = new BankAccount
                {
                    AccountNumber = model.AccountNumber,
                    OwnerName = model.OwnerName,
                    Balance = model.InitialBalance
                };

                var result = await _bankAccountService.CreateAsync(account);
                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.Message ?? "Could not create account.");
                    return View(model);
                }

                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (DataStoreException)
            {
                ModelState.AddModelError(string.Empty, "Cannot save account data right now. Please try again later.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber)) return NotFound();

            try
            {
                var account = await _bankAccountService.GetByAccountNumberAsync(accountNumber);
                if (account is null) return NotFound();

                return View(account);
            }
            catch (DataStoreException)
            {
                TempData["ErrorMessage"] = "Cannot read account details right now. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Freeze(string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                TempData["ErrorMessage"] = "Account number is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var result = await _bankAccountService.FreezeAsync(accountNumber);
                TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (DataStoreException)
            {
                TempData["ErrorMessage"] = "Cannot update account status right now. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unfreeze(string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                TempData["ErrorMessage"] = "Account number is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var result = await _bankAccountService.UnfreezeAsync(accountNumber);
                TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (DataStoreException)
            {
                TempData["ErrorMessage"] = "Cannot update account status right now. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
