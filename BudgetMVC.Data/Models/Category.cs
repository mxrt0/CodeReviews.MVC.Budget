using System.ComponentModel.DataAnnotations;


namespace BudgetMVC.Data.Models;

public class Category
{
    public int Id { get; set; }

    [Required]
    public required string Name { get; set; }

    public CategoryType Type { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

public enum CategoryType
{
    Income,
    Expense
}
