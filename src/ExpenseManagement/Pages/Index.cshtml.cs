using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IExpenseService expenseService, ILogger<IndexModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public DashboardStats Stats { get; set; } = new();
    public List<Expense> RecentExpenses { get; set; } = new();

    public async Task OnGetAsync()
    {
        var (stats, statsError) = await _expenseService.GetDashboardStatsAsync();
        Stats = stats;
        
        var (expenses, expensesError) = await _expenseService.GetExpensesAsync();
        RecentExpenses = expenses.Take(10).ToList();
        
        if (!string.IsNullOrEmpty(statsError))
        {
            ViewData["Error"] = statsError;
        }
        else if (!string.IsNullOrEmpty(expensesError))
        {
            ViewData["Error"] = expensesError;
        }
    }
}
