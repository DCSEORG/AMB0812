using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;
using System.ComponentModel.DataAnnotations;

namespace ExpenseManagement.Pages.Expenses;

public class CreateModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(IExpenseService expenseService, ILogger<CreateModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public List<Category> Categories { get; set; } = new();
    public List<User> Users { get; set; } = new();

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [Display(Name = "Employee")]
        public int UserId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime ExpenseDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDataAsync();
            return Page();
        }

        var request = new CreateExpenseRequest
        {
            UserId = Input.UserId,
            CategoryId = Input.CategoryId,
            Amount = Input.Amount,
            ExpenseDate = Input.ExpenseDate,
            Description = Input.Description
        };

        var (expenseId, error) = await _expenseService.CreateExpenseAsync(request);

        if (!string.IsNullOrEmpty(error))
        {
            ViewData["Error"] = error;
            await LoadDataAsync();
            return Page();
        }

        return RedirectToPage("Index");
    }

    private async Task LoadDataAsync()
    {
        var (categories, _) = await _expenseService.GetCategoriesAsync();
        Categories = categories;

        var (users, _) = await _expenseService.GetUsersAsync();
        Users = users;
    }
}
