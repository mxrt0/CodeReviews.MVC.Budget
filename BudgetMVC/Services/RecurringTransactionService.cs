using BudgetMVC.Data;
using BudgetMVC.Data.Models;

namespace BudgetMVC.Services;

public class RecurringTransactionService : BackgroundService
{
    private readonly IServiceProvider _services;
    public RecurringTransactionService(IServiceProvider services)
    {
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("\n\n\n\nRecurring transaction service is running...\n\n\n\n");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRecurringTransactionsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while processing recurring transactions: {ex.Message}");
            }
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }

    private async Task ProcessRecurringTransactionsAsync()
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();

        var today = DateTime.UtcNow.Date;
        var recurring = context.Transactions
            .Where(t => t.IsRecurring)
            .AsEnumerable()
            .Where(t =>
            {
                DateTime nextDue = t.Date.ToUniversalTime();
                switch (t.RecurrenceInterval!.ToLowerInvariant())
                {
                    case "daily": nextDue = nextDue.AddDays(1); break;
                    case "weekly": nextDue = nextDue.AddDays(7); break;
                    case "monthly": nextDue = nextDue.AddMonths(1); break;
                    case "yearly": nextDue = nextDue.AddYears(1); break;
                }
                return nextDue <= today;
            });
        foreach (var rec in recurring)
        {
            var newTransaction = new Transaction
            {
                Amount = rec.Amount,
                CategoryId = rec.CategoryId,
                Description = rec.Description,
                Date = today,
                Currency = rec.Currency,
                IsRecurring = true,
                RecurrenceInterval = rec.RecurrenceInterval,
            };
            context.Transactions.Add(newTransaction);
            rec.IsRecurring = false;
        }
        await context.SaveChangesAsync();
    }
}
