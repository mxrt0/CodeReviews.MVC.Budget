using BudgetMVC.Data;
using BudgetMVC.Data.Models;
using BudgetMVC.Models;
using BudgetMVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BudgetMVC.Controllers;

public class TransactionsController : Controller
{
    private readonly BudgetDbContext _context;
    private readonly DbCache _cache;
    private readonly string[] _currencies = { "USD", "EUR", "GBP" };
    private const int PageSize = 20;

    public TransactionsController(BudgetDbContext context, DbCache cache)
    {
        _context = context;
        _cache = cache;
    }

    private TransactionsViewModel SafeViewModel(
        List<Transaction>? transactions = null,
        int currentPage = 1,
        int totalPages = 1)
    {
        var categories = new List<Category>();
        try
        {
            categories = _cache.GetCategories().ToList();
        }
        catch
        {

        }

        return new TransactionsViewModel
        {
            NewTransaction = new Transaction(),
            Transactions = transactions ?? new List<Transaction>(),
            Categories = new SelectList(categories, "Id", "Name"),
            Currencies = new SelectList(_currencies, "USD"),
            CurrentPage = currentPage,
            TotalPages = totalPages
        };
    }

    public IActionResult Index(int page = 1)
    {
        try
        {
            if (!_context.Database.CanConnect())
            {
                TempData["ErrorMessage"] = "Database unavailable.";
                return View(SafeViewModel());
            }
            var totalCount = _cache.GetTransactions().Count();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));

            var transactions = _cache.GetTransactions()
                .OrderByDescending(t => t.IsRecurring)
                .ThenByDescending(t => t.Date)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return View(SafeViewModel(transactions, page, totalPages));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Unexpected error: {ex.Message}";
            return View(SafeViewModel());
        }
    }

    public IActionResult Search(string? q, string? categoryId, string? date, int page = 1)
    {
        try
        {
            if (!_context.Database.CanConnect())
            {
                TempData["ErrorMessage"] = "Database unavailable.";
                return View("Index", SafeViewModel());
            }

            var query = _cache.GetTransactions().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(t => EF.Functions.Like(t.Description, $"%{q}%"));

            if (int.TryParse(categoryId, out var parsedCat))
                query = query.Where(t => t.CategoryId == parsedCat);

            if (!string.IsNullOrWhiteSpace(date)
                && DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                query = query.Where(t => t.Date.Date == parsedDate.Date);

            if (string.IsNullOrWhiteSpace(q) && string.IsNullOrWhiteSpace(categoryId) && date is null)
                return RedirectToAction(nameof(Index));

            var totalCount = query.Count();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));

            var matches = query
                .OrderByDescending(t => t.IsRecurring)
                .ThenByDescending(t => t.Date)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return View("Index", SafeViewModel(matches, page, totalPages));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Search error: {ex.Message}";
            return View("Index", SafeViewModel());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "NewTransaction")] Transaction transaction, string? selectedCurrency)
    {
        try
        {
            if (!ModelState.IsValid)
                return View("Index", SafeViewModel());

            transaction.Currency = selectedCurrency ?? "USD";

            _context.Transactions.Add(transaction);
            _cache.ClearCache();
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Transaction added successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Failed to create transaction: {ex.Message}";
            return View("Index", SafeViewModel());
        }
    }

    [HttpGet]
    public IActionResult EditModalPartial(int id)
    {
        try
        {
            var record = _cache.GetTransactions().FirstOrDefault(r => r.Id == id);
            if (record is null) return NotFound();

            var vm = new EditTransactionViewModel
            {
                NewTransaction = new Transaction
                {
                    Date = record.Date,
                    CategoryId = record.CategoryId,
                    Amount = record.Amount,
                    Description = record.Description,
                    Currency = record.Currency
                },
                CurrentTransaction = record,
                Categories = new SelectList(_context.Categories, "Id", "Name", record.CategoryId),
                Currencies = new SelectList(_currencies, record.Currency)
            };
            return PartialView("_EditTransaction", vm);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Unable to open edit modal: {ex.Message}";
            return PartialView("_ErrorPartial");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, [Bind(Prefix = "NewTransaction")] Transaction newTransaction, string? selectedCurrency)
    {
        try
        {
            var recordToUpdate = await _context.Transactions.FindAsync(id);
            if (recordToUpdate is null) return NotFound();

            if (ModelState.IsValid)
            {
                recordToUpdate.Date = newTransaction.Date;
                recordToUpdate.Currency = selectedCurrency ?? recordToUpdate.Currency;
                recordToUpdate.Amount = newTransaction.Amount;
                recordToUpdate.Description = newTransaction.Description;

                _cache.ClearCache();
                await _context.SaveChangesAsync();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = true });

                TempData["SuccessMessage"] = $"Successfully edited transaction #{recordToUpdate.Id}!";
                return RedirectToAction(nameof(Index));
            }

            return PartialView("_EditTransaction", new EditTransactionViewModel
            {
                NewTransaction = newTransaction,
                CurrentTransaction = recordToUpdate,
                Categories = new SelectList(_context.Categories, "Id", "Name", newTransaction.CategoryId),
                Currencies = new SelectList(_currencies, newTransaction.Currency)
            });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Edit failed: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    public IActionResult DeleteModalPartial(int id)
    {
        try
        {
            var record = _cache.GetTransactions().FirstOrDefault(r => r.Id == id);
            if (record is null) return NotFound();
            return PartialView("_DeleteTransaction", record);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error loading delete modal: {ex.Message}";
            return PartialView("_ErrorPartial");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction is not null)
            {
                _context.Transactions.Remove(transaction);
                _cache.ClearCache();
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Successfully deleted transaction #{transaction.Id}!";
            }
            else
            {
                TempData["ErrorMessage"] = "Transaction not found.";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Delete failed: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }
}
