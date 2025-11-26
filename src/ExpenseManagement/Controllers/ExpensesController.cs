using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(IExpenseService expenseService, ILogger<ExpensesController> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all expenses with optional filtering
    /// </summary>
    /// <param name="userId">Filter by user ID</param>
    /// <param name="statusId">Filter by status ID (1=Draft, 2=Submitted, 3=Approved, 4=Rejected)</param>
    /// <param name="categoryId">Filter by category ID</param>
    /// <param name="searchTerm">Search in description, category, or user name</param>
    /// <returns>List of expenses</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<Expense>), 200)]
    public async Task<IActionResult> GetExpenses(
        [FromQuery] int? userId = null,
        [FromQuery] int? statusId = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] string? searchTerm = null)
    {
        var (expenses, error) = await _expenseService.GetExpensesAsync(userId, statusId, categoryId, searchTerm);
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error);
        }
        return Ok(expenses);
    }

    /// <summary>
    /// Gets a specific expense by ID
    /// </summary>
    /// <param name="id">The expense ID</param>
    /// <returns>The expense details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Expense), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetExpense(int id)
    {
        var (expense, error) = await _expenseService.GetExpenseByIdAsync(id);
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error);
        }
        if (expense == null)
        {
            return NotFound();
        }
        return Ok(expense);
    }

    /// <summary>
    /// Creates a new expense
    /// </summary>
    /// <param name="request">The expense creation request</param>
    /// <returns>The created expense ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(object), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        var (expenseId, error) = await _expenseService.CreateExpenseAsync(request);
        if (error != null)
        {
            return BadRequest(new { error });
        }
        return CreatedAtAction(nameof(GetExpense), new { id = expenseId }, new { expenseId });
    }

    /// <summary>
    /// Updates an existing expense (only Draft expenses can be updated)
    /// </summary>
    /// <param name="id">The expense ID</param>
    /// <param name="request">The update request</param>
    /// <returns>Success status</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdateExpense(int id, [FromBody] UpdateExpenseRequest request)
    {
        request.ExpenseId = id;
        var (success, error) = await _expenseService.UpdateExpenseAsync(request);
        if (!success)
        {
            return BadRequest(new { error });
        }
        return Ok(new { success = true });
    }

    /// <summary>
    /// Deletes an expense (only Draft expenses can be deleted)
    /// </summary>
    /// <param name="id">The expense ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> DeleteExpense(int id)
    {
        var (success, error) = await _expenseService.DeleteExpenseAsync(id);
        if (!success)
        {
            return BadRequest(new { error });
        }
        return Ok(new { success = true });
    }

    /// <summary>
    /// Submits a draft expense for approval
    /// </summary>
    /// <param name="id">The expense ID</param>
    /// <returns>Success status</returns>
    [HttpPost("{id}/submit")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SubmitExpense(int id)
    {
        var (success, error) = await _expenseService.SubmitExpenseAsync(id);
        if (!success)
        {
            return BadRequest(new { error });
        }
        return Ok(new { success = true });
    }

    /// <summary>
    /// Approves a submitted expense (manager action)
    /// </summary>
    /// <param name="id">The expense ID</param>
    /// <param name="reviewerId">The reviewer's user ID</param>
    /// <returns>Success status</returns>
    [HttpPost("{id}/approve")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ApproveExpense(int id, [FromQuery] int reviewerId)
    {
        var (success, error) = await _expenseService.ApproveExpenseAsync(id, reviewerId);
        if (!success)
        {
            return BadRequest(new { error });
        }
        return Ok(new { success = true });
    }

    /// <summary>
    /// Rejects a submitted expense (manager action)
    /// </summary>
    /// <param name="id">The expense ID</param>
    /// <param name="reviewerId">The reviewer's user ID</param>
    /// <returns>Success status</returns>
    [HttpPost("{id}/reject")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> RejectExpense(int id, [FromQuery] int reviewerId)
    {
        var (success, error) = await _expenseService.RejectExpenseAsync(id, reviewerId);
        if (!success)
        {
            return BadRequest(new { error });
        }
        return Ok(new { success = true });
    }

    /// <summary>
    /// Gets expenses pending approval
    /// </summary>
    /// <param name="searchTerm">Optional search term</param>
    /// <returns>List of pending expenses</returns>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<Expense>), 200)]
    public async Task<IActionResult> GetPendingApprovals([FromQuery] string? searchTerm = null)
    {
        var (expenses, error) = await _expenseService.GetPendingApprovalsAsync(searchTerm);
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error);
        }
        return Ok(expenses);
    }

    /// <summary>
    /// Gets dashboard statistics
    /// </summary>
    /// <returns>Dashboard statistics</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(DashboardStats), 200)]
    public async Task<IActionResult> GetDashboardStats()
    {
        var (stats, error) = await _expenseService.GetDashboardStatsAsync();
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error);
        }
        return Ok(stats);
    }

    /// <summary>
    /// Gets all expense categories
    /// </summary>
    /// <returns>List of categories</returns>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(List<Category>), 200)]
    public async Task<IActionResult> GetCategories()
    {
        var (categories, error) = await _expenseService.GetCategoriesAsync();
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error);
        }
        return Ok(categories);
    }

    /// <summary>
    /// Gets all expense statuses
    /// </summary>
    /// <returns>List of statuses</returns>
    [HttpGet("statuses")]
    [ProducesResponseType(typeof(List<ExpenseStatus>), 200)]
    public async Task<IActionResult> GetStatuses()
    {
        var (statuses, error) = await _expenseService.GetStatusesAsync();
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error);
        }
        return Ok(statuses);
    }

    /// <summary>
    /// Gets all users
    /// </summary>
    /// <returns>List of users</returns>
    [HttpGet("users")]
    [ProducesResponseType(typeof(List<User>), 200)]
    public async Task<IActionResult> GetUsers()
    {
        var (users, error) = await _expenseService.GetUsersAsync();
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error);
        }
        return Ok(users);
    }
}
