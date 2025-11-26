using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;
using System.ComponentModel.DataAnnotations;

namespace ExpenseManagement.Pages.Expenses;

public class EditModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(IExpenseService expenseService, ILogger<EditModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public List<Category> Categories { get; set; } = new();
    public Expense? Expense { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public int ExpenseId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime ExpenseDate { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var (expense, error) = await _expenseService.GetExpenseByIdAsync(id);
        
        if (expense == null)
        {
            return NotFound();
        }

        Expense = expense;
        Input = new InputModel
        {
            ExpenseId = expense.ExpenseId,
            Amount = expense.AmountGBP,
            ExpenseDate = expense.ExpenseDate,
            CategoryId = expense.CategoryId,
            Description = expense.Description
        };

        await LoadCategoriesAsync();

        if (!string.IsNullOrEmpty(error))
        {
            ViewData["Error"] = error;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadExpenseAndCategoriesAsync(Input.ExpenseId);
            return Page();
        }

        var request = new UpdateExpenseRequest
        {
            ExpenseId = Input.ExpenseId,
            CategoryId = Input.CategoryId,
            Amount = Input.Amount,
            ExpenseDate = Input.ExpenseDate,
            Description = Input.Description
        };

        var (success, error) = await _expenseService.UpdateExpenseAsync(request);

        if (!success)
        {
            ViewData["Error"] = error ?? "Failed to update expense";
            await LoadExpenseAndCategoriesAsync(Input.ExpenseId);
            return Page();
        }

        return RedirectToPage("Index");
    }

    private async Task LoadCategoriesAsync()
    {
        var (categories, _) = await _expenseService.GetCategoriesAsync();
        Categories = categories;
    }

    private async Task LoadExpenseAndCategoriesAsync(int id)
    {
        var (expense, _) = await _expenseService.GetExpenseByIdAsync(id);
        Expense = expense;
        await LoadCategoriesAsync();
    }
}
