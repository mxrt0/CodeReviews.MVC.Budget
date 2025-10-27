using BudgetMVC.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace BudgetMVC.Models
{
    public class EditTransactionViewModel
    {
        public Transaction NewTransaction { get; set; }
        public Transaction? CurrentTransaction { get; set; }
        public SelectList Categories { get; set; }
        public SelectList Currencies { get; set; }
        public string? SelectedCurrency { get; set; }
    }
}
