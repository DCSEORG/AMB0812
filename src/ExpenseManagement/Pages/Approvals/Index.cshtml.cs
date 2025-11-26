using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages.Approvals;

public class IndexModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IExpenseService expenseService, ILogger<IndexModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public List<Expense> PendingExpenses { get; set; } = new();
    public int CurrentReviewerId { get; set; } = 2; // Default to Bob Manager for demo

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync()
    {
        var (expenses, error) = await _expenseService.GetPendingApprovalsAsync(SearchTerm);
        PendingExpenses = expenses;

        if (!string.IsNullOrEmpty(error))
        {
            ViewData["Error"] = error;
        }
    }

    public async Task<IActionResult> OnPostApproveAsync(int id, int reviewerId)
    {
        var (success, error) = await _expenseService.ApproveExpenseAsync(id, reviewerId);
        if (!success && !string.IsNullOrEmpty(error))
        {
            TempData["Error"] = error;
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int id, int reviewerId)
    {
        var (success, error) = await _expenseService.RejectExpenseAsync(id, reviewerId);
        if (!success && !string.IsNullOrEmpty(error))
        {
            TempData["Error"] = error;
        }
        return RedirectToPage();
    }
}
