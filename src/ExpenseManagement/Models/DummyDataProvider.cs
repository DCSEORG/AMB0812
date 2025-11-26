namespace ExpenseManagement.Models;

public class DummyDataProvider
{
    public static List<Expense> GetDummyExpenses()
    {
        return new List<Expense>
        {
            new Expense
            {
                ExpenseId = 1,
                UserId = 1,
                UserName = "Alice Example",
                Email = "alice@example.co.uk",
                CategoryId = 1,
                CategoryName = "Travel",
                StatusId = 3,
                StatusName = "Approved",
                AmountMinor = 12300,
                AmountGBP = 123.00m,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-8),
                Description = "Travel for meeting",
                SubmittedAt = DateTime.Now.AddDays(-7),
                ReviewedBy = 2,
                ReviewerName = "Bob Manager",
                ReviewedAt = DateTime.Now.AddDays(-6),
                CreatedAt = DateTime.Now.AddDays(-8)
            },
            new Expense
            {
                ExpenseId = 2,
                UserId = 1,
                UserName = "Alice Example",
                Email = "alice@example.co.uk",
                CategoryId = 3,
                CategoryName = "Supplies",
                StatusId = 3,
                StatusName = "Approved",
                AmountMinor = 100,
                AmountGBP = 1.00m,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-6),
                Description = "Office supplies",
                SubmittedAt = DateTime.Now.AddDays(-5),
                ReviewedBy = 2,
                ReviewerName = "Bob Manager",
                ReviewedAt = DateTime.Now.AddDays(-4),
                CreatedAt = DateTime.Now.AddDays(-6)
            },
            new Expense
            {
                ExpenseId = 3,
                UserId = 2,
                UserName = "Bob Manager",
                Email = "bob.manager@example.co.uk",
                CategoryId = 1,
                CategoryName = "Travel",
                StatusId = 1,
                StatusName = "Draft",
                AmountMinor = 23400,
                AmountGBP = 234.00m,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-15),
                Description = "Client visit travel",
                CreatedAt = DateTime.Now.AddDays(-15)
            },
            new Expense
            {
                ExpenseId = 4,
                UserId = 1,
                UserName = "Alice Example",
                Email = "alice@example.co.uk",
                CategoryId = 1,
                CategoryName = "Travel",
                StatusId = 2,
                StatusName = "Submitted",
                AmountMinor = 25000,
                AmountGBP = 250.00m,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-21),
                Description = "Client dinner meeting",
                SubmittedAt = DateTime.Now.AddDays(-20),
                CreatedAt = DateTime.Now.AddDays(-21)
            },
            new Expense
            {
                ExpenseId = 5,
                UserId = 1,
                UserName = "Alice Example",
                Email = "alice@example.co.uk",
                CategoryId = 2,
                CategoryName = "Meals",
                StatusId = 3,
                StatusName = "Approved",
                AmountMinor = 5500,
                AmountGBP = 55.00m,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-30),
                Description = "Team lunch",
                SubmittedAt = DateTime.Now.AddDays(-29),
                ReviewedBy = 2,
                ReviewerName = "Bob Manager",
                ReviewedAt = DateTime.Now.AddDays(-28),
                CreatedAt = DateTime.Now.AddDays(-30)
            }
        };
    }

    public static List<Category> GetDummyCategories()
    {
        return new List<Category>
        {
            new Category { CategoryId = 1, CategoryName = "Travel", IsActive = true },
            new Category { CategoryId = 2, CategoryName = "Meals", IsActive = true },
            new Category { CategoryId = 3, CategoryName = "Supplies", IsActive = true },
            new Category { CategoryId = 4, CategoryName = "Accommodation", IsActive = true },
            new Category { CategoryId = 5, CategoryName = "Other", IsActive = true }
        };
    }

    public static List<ExpenseStatus> GetDummyStatuses()
    {
        return new List<ExpenseStatus>
        {
            new ExpenseStatus { StatusId = 1, StatusName = "Draft" },
            new ExpenseStatus { StatusId = 2, StatusName = "Submitted" },
            new ExpenseStatus { StatusId = 3, StatusName = "Approved" },
            new ExpenseStatus { StatusId = 4, StatusName = "Rejected" }
        };
    }

    public static List<User> GetDummyUsers()
    {
        return new List<User>
        {
            new User
            {
                UserId = 1,
                UserName = "Alice Example",
                Email = "alice@example.co.uk",
                RoleId = 1,
                RoleName = "Employee",
                ManagerId = 2,
                ManagerName = "Bob Manager",
                IsActive = true,
                CreatedAt = DateTime.Now.AddMonths(-6)
            },
            new User
            {
                UserId = 2,
                UserName = "Bob Manager",
                Email = "bob.manager@example.co.uk",
                RoleId = 2,
                RoleName = "Manager",
                IsActive = true,
                CreatedAt = DateTime.Now.AddMonths(-12)
            }
        };
    }

    public static DashboardStats GetDummyDashboardStats()
    {
        return new DashboardStats
        {
            TotalExpenses = 10,
            PendingApprovals = 1,
            ApprovedAmountMinor = 51924,
            ApprovedCount = 6
        };
    }
}
