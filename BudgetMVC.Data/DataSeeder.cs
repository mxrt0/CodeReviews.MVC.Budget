using BudgetMVC.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetMVC.Data;

public static class DataSeeder
{
    private static readonly Random _random = new Random();

    private static readonly string[] _incomeCategories =
    {
        "Salary", "Bonus", "Investment", "Freelance", "Gift", "Refund"
    };

    private static readonly string[] _expenseCategories =
    {
        "Groceries", "Transport", "Entertainment", "Utilities",
        "Dining", "Health", "Rent", "Shopping", "Education", "Travel"
    };

    private static readonly string[] _currencies = { "USD", "EUR", "GBP" };

    private static readonly string[] _descriptions =
    {
        "Monthly payment", "Dinner with friends", "Grocery shopping",
        "Movie night", "Gym membership", "Online course", "Freelance project",
        "Stock dividend", "Birthday gift", "Taxi ride", "Utility bill"
    };
    public static async Task SeedData(IServiceProvider serviceProvider)
    {
        var context = new BudgetDbContext(serviceProvider
            .GetRequiredService<DbContextOptions<BudgetDbContext>>());
        if (context.Transactions.Any() || context.Categories.Any())
        {
            return;
        }

        var categories = new List<Category>();

        categories.AddRange(_incomeCategories.Select(name => new Category
        {
            Name = name,
            Type = CategoryType.Income
        }));

        categories.AddRange(_expenseCategories.Select(name => new Category
        {
            Name = name,
            Type = CategoryType.Expense
        }));

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();

        var allCategoryIds = context.Categories.Select(c => c.Id).ToList();

        var transactions = new List<Transaction>();

        for (int i = 0; i < 1000; i++)
        {
            var categoryId = allCategoryIds[_random.Next(allCategoryIds.Count)];
            var category = categories.First(c => c.Id == categoryId);
            var isIncome = category.Type == CategoryType.Income;

            var amount = Math.Round((decimal)(_random.NextDouble() * (isIncome ? 2000 : 500) + 10), 2);
            var date = DateTime.Now.AddDays(-_random.Next(0, 365));
            var currency = _currencies[_random.Next(_currencies.Length)];
            var description = _descriptions[_random.Next(_descriptions.Length)];

            transactions.Add(new Transaction
            {
                Amount = amount,
                Date = date,
                Currency = currency,
                Description = description,
                CategoryId = categoryId
            });
        }

        await context.Transactions.AddRangeAsync(transactions);
        await context.SaveChangesAsync();

    }
}
