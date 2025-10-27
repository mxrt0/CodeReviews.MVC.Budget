namespace BudgetMVC.Services;

using BudgetMVC.Data;
using BudgetMVC.Data.Models;
using Microsoft.EntityFrameworkCore;
public class DbCache
{
    private IEnumerable<Transaction>? _transactionCache;
    private IEnumerable<Category>? _categoryCache;
    private readonly Func<BudgetDbContext> _dbFactory;
    private readonly object _lock = new object();

    public DbCache(Func<BudgetDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public IEnumerable<Transaction> GetTransactions()
    {
        lock (_lock)
        {
            if (_transactionCache is not null)
                return _transactionCache;

            using var db = _dbFactory();
            _transactionCache = db.Transactions.Include(t => t.Category).AsNoTracking().ToList();
            return _transactionCache;
        }
    }

    public IEnumerable<Category> GetCategories()
    {
        lock (_lock)
        {
            if (_categoryCache is not null)
                return _categoryCache;

            using var db = _dbFactory();
            _categoryCache = db.Categories.AsNoTracking().ToList();
            return _categoryCache;
        }
    }

    public void ClearCache()
    {
        lock (_lock)
        {
            _transactionCache = null;
            _categoryCache = null;
        }
    }
}

