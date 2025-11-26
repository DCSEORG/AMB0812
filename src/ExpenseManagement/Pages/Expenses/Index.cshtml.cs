using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages.Expenses;

public class IndexModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IExpenseService expenseService, ILogger<IndexModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public List<Expense> Expenses { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<ExpenseStatus> Statuses { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? StatusId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync()
    {
        var (expenses, expensesError) = await _expenseService.GetExpensesAsync(null, StatusId, CategoryId, SearchTerm);
        Expenses = expenses;

        var (categories, _) = await _expenseService.GetCategoriesAsync();
        Categories = categories;

        var (statuses, _) = await _expenseService.GetStatusesAsync();
        Statuses = statuses;

        if (!string.IsNullOrEmpty(expensesError))
        {
            ViewData["Error"] = expensesError;
        }
    }

    public async Task<IActionResult> OnPostSubmitAsync(int id)
    {
        var (success, error) = await _expenseService.SubmitExpenseAsync(id);
        if (!success && !string.IsNullOrEmpty(error))
        {
            TempData["Error"] = error;
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var (success, error) = await _expenseService.DeleteExpenseAsync(id);
        if (!success && !string.IsNullOrEmpty(error))
        {
            TempData["Error"] = error;
        }
        return RedirectToPage();
    }
}
