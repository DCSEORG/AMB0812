using Microsoft.Data.SqlClient;
using ExpenseManagement.Models;

namespace ExpenseManagement.Services;

public interface IExpenseService
{
    Task<(List<Expense> Expenses, string? Error)> GetExpensesAsync(int? userId = null, int? statusId = null, int? categoryId = null, string? searchTerm = null);
    Task<(Expense? Expense, string? Error)> GetExpenseByIdAsync(int expenseId);
    Task<(int? ExpenseId, string? Error)> CreateExpenseAsync(CreateExpenseRequest request);
    Task<(bool Success, string? Error)> UpdateExpenseAsync(UpdateExpenseRequest request);
    Task<(bool Success, string? Error)> DeleteExpenseAsync(int expenseId);
    Task<(bool Success, string? Error)> SubmitExpenseAsync(int expenseId);
    Task<(bool Success, string? Error)> ApproveExpenseAsync(int expenseId, int reviewerId);
    Task<(bool Success, string? Error)> RejectExpenseAsync(int expenseId, int reviewerId);
    Task<(List<Expense> Expenses, string? Error)> GetPendingApprovalsAsync(string? searchTerm = null);
    Task<(List<Category> Categories, string? Error)> GetCategoriesAsync();
    Task<(List<ExpenseStatus> Statuses, string? Error)> GetStatusesAsync();
    Task<(List<User> Users, string? Error)> GetUsersAsync();
    Task<(User? User, string? Error)> GetUserByIdAsync(int userId);
    Task<(DashboardStats Stats, string? Error)> GetDashboardStatsAsync();
}

public class ExpenseService : IExpenseService
{
    private readonly string _connectionString;
    private readonly ILogger<ExpenseService> _logger;

    public ExpenseService(IConfiguration configuration, ILogger<ExpenseService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    private async Task<SqlConnection> GetConnectionAsync()
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    public async Task<(List<Expense> Expenses, string? Error)> GetExpensesAsync(int? userId = null, int? statusId = null, int? categoryId = null, string? searchTerm = null)
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.sp_GetExpenses", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@UserId", userId.HasValue ? userId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@StatusId", statusId.HasValue ? statusId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@CategoryId", categoryId.HasValue ? categoryId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@SearchTerm", string.IsNullOrEmpty(searchTerm) ? DBNull.Value : searchTerm);

            var expenses = new List<Expense>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }

            return (expenses, null);
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "GetExpensesAsync", "Services/ExpenseService.cs", 58);
            _logger.LogError(ex, error);
            return (DummyDataProvider.GetDummyExpenses(), error);
        }
    }

    public async Task<(Expense? Expense, string? Error)> GetExpenseByIdAsync(int expenseId)
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.sp_GetExpenseById", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (MapExpense(reader), null);
            }
            return (null, null);
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "GetExpenseByIdAsync", "Services/ExpenseService.cs", 78);
            _logger.LogError(ex, error);
            return (DummyDataProvider.GetDummyExpenses().FirstOrDefault(e => e.ExpenseId == expenseId), error);
        }
    }

    public async Task<(int? ExpenseId, string? Error)> CreateExpenseAsync(CreateExpenseRequest request)
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.sp_CreateExpense", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@UserId", request.UserId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)(request.Amount * 100));
            command.Parameters.AddWithValue("@Currency", "GBP");
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(request.Description) ? DBNull.Value : request.Description);
            command.Parameters.AddWithValue("@ReceiptFile", string.IsNullOrEmpty(request.ReceiptFile) ? DBNull.Value : request.ReceiptFile);

            var result = await command.ExecuteScalarAsync();
            return (Convert.ToInt32(result), null);
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "CreateExpenseAsync", "Services/ExpenseService.cs", 103);
            _logger.LogError(ex, error);
            return (null, error);
        }
    }

    public async Task<(bool Success, string? Error)> UpdateExpenseAsync(UpdateExpenseRequest request)
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.sp_UpdateExpense", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@ExpenseId", request.ExpenseId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)(request.Amount * 100));
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(request.Description) ? DBNull.Value : request.Description);
            command.Parameters.AddWithValue("@ReceiptFile", string.IsNullOrEmpty(request.ReceiptFile) ? DBNull.Value : request.ReceiptFile);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (reader.GetInt32(0) > 0, null);
            }
            return (false, "Expense not found or cannot be updated (only Draft expenses can be modified).");
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "UpdateExpenseAsync", "Services/ExpenseService.cs", 131);
            _logger.LogError(ex, error);
            return (false, error);
        }
    }

    public async Task<(bool Success, string? Error)> DeleteExpenseAsync(int expenseId)
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.sp_DeleteExpense", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (reader.GetInt32(0) > 0, null);
            }
            return (false, "Expense not found or cannot be deleted (only Draft expenses can be deleted).");
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "DeleteExpenseAsync", "Services/ExpenseService.cs", 153);
            _logger.LogError(ex, error);
            return (false, error);
        }
    }

    public async Task<(bool Success, string? Error)> SubmitExpenseAsync(int expenseId)
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.sp_SubmitExpense", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (reader.GetInt32(0) > 0, null);
            }
            return (false, "Expense not found or cannot be submitted (only Draft expenses can be submitted).");
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "SubmitExpenseAsync", "Services/ExpenseService.cs", 175);
            _logger.LogError(ex, error);
            return (false, error);
        }
    }

    public async Task<(bool Success, string? Error)> ApproveExpenseAsync(int expenseId, int reviewerId)
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.sp_ApproveExpense", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewerId", reviewerId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (reader.GetInt32(0) > 0, null);
            }
            return (false, "Expense not found or cannot be approved (only Submitted expenses can be approved).");
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "ApproveExpenseAsync", "Services/ExpenseService.cs", 198);
            _logger.LogError(ex, error);
            return (false, error);
        }
    }

    public async Task<(bool Success, string? Error)> RejectExpenseAsync(int expenseId, int reviewerId)
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.sp_RejectExpense", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewerId", reviewerId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (reader.GetInt32(0) > 0, null);
            }
            return (false, "Expense not found or cannot be rejected (only Submitted expenses can be rejected).");
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "RejectExpenseAsync", "Services/ExpenseService.cs", 221);
            _logger.LogError(ex, error);
            return (false, error);
        }
    }

    public async Task<(List<Expense> Expenses, string? Error)> GetPendingApprovalsAsync(string? searchTerm = null)
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.sp_GetPendingApprovals", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@SearchTerm", string.IsNullOrEmpty(searchTerm) ? DBNull.Value : searchTerm);

            var expenses = new List<Expense>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }

            return (expenses, null);
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "GetPendingApprovalsAsync", "Services/ExpenseService.cs", 244);
            _logger.LogError(ex, error);
            return (DummyDataProvider.GetDummyExpenses().Where(e => e.StatusId == 2).ToList(), error);
        }
    }

    public async Task<(List<Category> Categories, string? Error)> GetCategoriesAsync()
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.sp_GetCategories", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            var categories = new List<Category>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new Category
                {
                    CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                });
            }

            return (categories, null);
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "GetCategoriesAsync", "Services/ExpenseService.cs", 272);
            _logger.LogError(ex, error);
            return (DummyDataProvider.GetDummyCategories(), error);
        }
    }

    public async Task<(List<ExpenseStatus> Statuses, string? Error)> GetStatusesAsync()
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.sp_GetStatuses", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            var statuses = new List<ExpenseStatus>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                statuses.Add(new ExpenseStatus
                {
                    StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
                    StatusName = reader.GetString(reader.GetOrdinal("StatusName"))
                });
            }

            return (statuses, null);
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "GetStatusesAsync", "Services/ExpenseService.cs", 298);
            _logger.LogError(ex, error);
            return (DummyDataProvider.GetDummyStatuses(), error);
        }
    }

    public async Task<(List<User> Users, string? Error)> GetUsersAsync()
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.sp_GetUsers", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            var users = new List<User>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(MapUser(reader));
            }

            return (users, null);
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "GetUsersAsync", "Services/ExpenseService.cs", 320);
            _logger.LogError(ex, error);
            return (DummyDataProvider.GetDummyUsers(), error);
        }
    }

    public async Task<(User? User, string? Error)> GetUserByIdAsync(int userId)
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.sp_GetUserById", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@UserId", userId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (MapUser(reader), null);
            }
            return (null, null);
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "GetUserByIdAsync", "Services/ExpenseService.cs", 341);
            _logger.LogError(ex, error);
            return (DummyDataProvider.GetDummyUsers().FirstOrDefault(u => u.UserId == userId), error);
        }
    }

    public async Task<(DashboardStats Stats, string? Error)> GetDashboardStatsAsync()
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.sp_GetDashboardStats", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (new DashboardStats
                {
                    TotalExpenses = reader.GetInt32(reader.GetOrdinal("TotalExpenses")),
                    PendingApprovals = reader.GetInt32(reader.GetOrdinal("PendingApprovals")),
                    ApprovedAmountMinor = reader.GetInt32(reader.GetOrdinal("ApprovedAmountMinor")),
                    ApprovedCount = reader.GetInt32(reader.GetOrdinal("ApprovedCount"))
                }, null);
            }
            return (DummyDataProvider.GetDummyDashboardStats(), null);
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "GetDashboardStatsAsync", "Services/ExpenseService.cs", 367);
            _logger.LogError(ex, error);
            return (DummyDataProvider.GetDummyDashboardStats(), error);
        }
    }

    private static Expense MapExpense(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(reader.GetOrdinal("ExpenseId")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
            CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
            StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
            StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
            AmountMinor = reader.GetInt32(reader.GetOrdinal("AmountMinor")),
            AmountGBP = reader.GetDecimal(reader.GetOrdinal("AmountGBP")),
            Currency = reader.GetString(reader.GetOrdinal("Currency")),
            ExpenseDate = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            ReceiptFile = reader.IsDBNull(reader.GetOrdinal("ReceiptFile")) ? null : reader.GetString(reader.GetOrdinal("ReceiptFile")),
            SubmittedAt = reader.IsDBNull(reader.GetOrdinal("SubmittedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
            ReviewedBy = reader.IsDBNull(reader.GetOrdinal("ReviewedBy")) ? null : reader.GetInt32(reader.GetOrdinal("ReviewedBy")),
            ReviewerName = HasColumn(reader, "ReviewerName") && !reader.IsDBNull(reader.GetOrdinal("ReviewerName")) ? reader.GetString(reader.GetOrdinal("ReviewerName")) : null,
            ReviewedAt = reader.IsDBNull(reader.GetOrdinal("ReviewedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ReviewedAt")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    private static User MapUser(SqlDataReader reader)
    {
        return new User
        {
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
            RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
            ManagerId = reader.IsDBNull(reader.GetOrdinal("ManagerId")) ? null : reader.GetInt32(reader.GetOrdinal("ManagerId")),
            ManagerName = reader.IsDBNull(reader.GetOrdinal("ManagerName")) ? null : reader.GetString(reader.GetOrdinal("ManagerName")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    private static bool HasColumn(SqlDataReader reader, string columnName)
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static string FormatError(Exception ex, string methodName, string fileName, int lineNumber)
    {
        var errorMessage = $"Database connection error in {methodName} at {fileName}:{lineNumber}. ";
        
        if (ex.Message.Contains("managed identity", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("token", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage += "Managed Identity authentication issue detected. " +
                "Fix: 1) Ensure the User-Assigned Managed Identity is assigned to the App Service. " +
                "2) Verify the AZURE_CLIENT_ID environment variable is set to the Managed Identity's Client ID. " +
                "3) Ensure the Managed Identity has been added as a database user with appropriate roles (db_datareader, db_datawriter). " +
                "4) Wait a few minutes after role assignment for changes to propagate. ";
        }
        
        errorMessage += $"Error: {ex.Message}";
        
        return errorMessage;
    }
}
