using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using ExpenseManagement.Models;
using System.Text.Json;

namespace ExpenseManagement.Services;

public interface IChatService
{
    Task<ChatResponse> ProcessMessageAsync(ChatRequest request);
    bool IsConfigured { get; }
}

public class ChatService : IChatService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatService> _logger;
    private readonly IExpenseService _expenseService;
    private readonly OpenAIClient? _openAIClient;
    private readonly string? _deploymentName;

    public bool IsConfigured => _openAIClient != null && !string.IsNullOrEmpty(_deploymentName);

    public ChatService(IConfiguration configuration, ILogger<ChatService> logger, IExpenseService expenseService)
    {
        _configuration = configuration;
        _logger = logger;
        _expenseService = expenseService;

        var endpoint = _configuration["OpenAI:Endpoint"];
        _deploymentName = _configuration["OpenAI:DeploymentName"];

        if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(_deploymentName))
        {
            try
            {
                var managedIdentityClientId = _configuration["ManagedIdentityClientId"];
                TokenCredential credential;

                if (!string.IsNullOrEmpty(managedIdentityClientId))
                {
                    _logger.LogInformation("Using ManagedIdentityCredential with client ID: {ClientId}", managedIdentityClientId);
                    credential = new ManagedIdentityCredential(managedIdentityClientId);
                }
                else
                {
                    _logger.LogInformation("Using DefaultAzureCredential");
                    credential = new DefaultAzureCredential();
                }

                _openAIClient = new OpenAIClient(new Uri(endpoint), credential);
                _logger.LogInformation("OpenAI client initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize OpenAI client");
            }
        }
        else
        {
            _logger.LogWarning("OpenAI configuration is incomplete. Chat functionality will return dummy responses.");
        }
    }

    public async Task<ChatResponse> ProcessMessageAsync(ChatRequest request)
    {
        if (!IsConfigured)
        {
            return new ChatResponse
            {
                Response = "🤖 **GenAI services are not configured.**\n\n" +
                          "To enable intelligent chat features, please deploy the GenAI resources using:\n\n" +
                          "```bash\n./deploy-with-chat.sh\n```\n\n" +
                          "This will set up Azure OpenAI and enable natural language interactions with your expense data.",
                Success = true
            };
        }

        try
        {
            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(GetSystemPrompt())
                },
                MaxTokens = 2000,
                Temperature = 0.7f
            };

            // Add function tools
            foreach (var tool in GetFunctionTools())
            {
                chatCompletionsOptions.Tools.Add(tool);
            }

            // Add conversation history
            if (request.History != null)
            {
                foreach (var msg in request.History)
                {
                    if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                        chatCompletionsOptions.Messages.Add(new ChatRequestUserMessage(msg.Content));
                    else if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                        chatCompletionsOptions.Messages.Add(new ChatRequestAssistantMessage(msg.Content));
                }
            }

            chatCompletionsOptions.Messages.Add(new ChatRequestUserMessage(request.Message));

            // Function calling loop
            var maxIterations = 5;
            var iteration = 0;

            while (iteration < maxIterations)
            {
                iteration++;
                var response = await _openAIClient!.GetChatCompletionsAsync(chatCompletionsOptions);
                var choice = response.Value.Choices[0];

                if (choice.FinishReason == CompletionsFinishReason.ToolCalls)
                {
                    var assistantMessage = new ChatRequestAssistantMessage(choice.Message);
                    chatCompletionsOptions.Messages.Add(assistantMessage);

                    foreach (var toolCall in choice.Message.ToolCalls)
                    {
                        if (toolCall is ChatCompletionsFunctionToolCall functionToolCall)
                        {
                            var functionResult = await ExecuteFunctionAsync(functionToolCall.Name, functionToolCall.Arguments);
                            chatCompletionsOptions.Messages.Add(new ChatRequestToolMessage(functionResult, functionToolCall.Id));
                        }
                    }
                }
                else
                {
                    return new ChatResponse
                    {
                        Response = choice.Message.Content ?? "I couldn't process your request.",
                        Success = true
                    };
                }
            }

            return new ChatResponse
            {
                Response = "I encountered an issue processing your request. Please try again.",
                Success = false,
                Error = "Maximum function call iterations reached"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return new ChatResponse
            {
                Response = "I'm sorry, I encountered an error processing your request.",
                Success = false,
                Error = ex.Message
            };
        }
    }

    private string GetSystemPrompt()
    {
        return @"You are an intelligent assistant for the Expense Management System. You help users manage their expenses, view reports, and understand their spending patterns.

You have access to the following functions to interact with the expense database:
- get_expenses: Retrieve expenses with optional filters
- get_expense_by_id: Get details of a specific expense
- create_expense: Create a new expense
- submit_expense: Submit an expense for approval
- get_pending_approvals: Get expenses pending approval
- approve_expense: Approve a submitted expense
- reject_expense: Reject a submitted expense
- get_dashboard_stats: Get overall statistics

When users ask about their expenses or want to perform actions:
1. Use the appropriate function to get or modify data
2. Present the results in a clear, formatted way
3. Use bullet points or numbered lists for multiple items
4. Format currency values as £X.XX
5. Be helpful and suggest related actions the user might want to take

If the user asks about something outside the expense system, politely redirect them to expense-related topics.";
    }

    private ChatCompletionsFunctionToolDefinition[] GetFunctionTools()
    {
        return new[]
        {
            new ChatCompletionsFunctionToolDefinition(new FunctionDefinition
            {
                Name = "get_expenses",
                Description = "Retrieves a list of expenses. Can filter by user, status, category, or search term.",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        userId = new { type = "integer", description = "Filter by user ID" },
                        statusId = new { type = "integer", description = "Filter by status ID (1=Draft, 2=Submitted, 3=Approved, 4=Rejected)" },
                        categoryId = new { type = "integer", description = "Filter by category ID (1=Travel, 2=Meals, 3=Supplies, 4=Accommodation, 5=Other)" },
                        searchTerm = new { type = "string", description = "Search in description, category, or user name" }
                    }
                })
            }),
            new ChatCompletionsFunctionToolDefinition(new FunctionDefinition
            {
                Name = "get_expense_by_id",
                Description = "Gets detailed information about a specific expense by its ID.",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        expenseId = new { type = "integer", description = "The ID of the expense to retrieve" }
                    },
                    required = new[] { "expenseId" }
                })
            }),
            new ChatCompletionsFunctionToolDefinition(new FunctionDefinition
            {
                Name = "create_expense",
                Description = "Creates a new expense entry.",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        userId = new { type = "integer", description = "The user ID who is creating the expense" },
                        categoryId = new { type = "integer", description = "Category ID (1=Travel, 2=Meals, 3=Supplies, 4=Accommodation, 5=Other)" },
                        amount = new { type = "number", description = "Amount in GBP" },
                        expenseDate = new { type = "string", description = "Date of the expense in YYYY-MM-DD format" },
                        description = new { type = "string", description = "Description of the expense" }
                    },
                    required = new[] { "userId", "categoryId", "amount", "expenseDate" }
                })
            }),
            new ChatCompletionsFunctionToolDefinition(new FunctionDefinition
            {
                Name = "submit_expense",
                Description = "Submits a draft expense for approval.",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        expenseId = new { type = "integer", description = "The ID of the expense to submit" }
                    },
                    required = new[] { "expenseId" }
                })
            }),
            new ChatCompletionsFunctionToolDefinition(new FunctionDefinition
            {
                Name = "get_pending_approvals",
                Description = "Gets all expenses that are waiting for approval.",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        searchTerm = new { type = "string", description = "Optional search term to filter pending approvals" }
                    }
                })
            }),
            new ChatCompletionsFunctionToolDefinition(new FunctionDefinition
            {
                Name = "approve_expense",
                Description = "Approves a submitted expense. Only managers can do this.",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        expenseId = new { type = "integer", description = "The ID of the expense to approve" },
                        reviewerId = new { type = "integer", description = "The user ID of the manager approving" }
                    },
                    required = new[] { "expenseId", "reviewerId" }
                })
            }),
            new ChatCompletionsFunctionToolDefinition(new FunctionDefinition
            {
                Name = "reject_expense",
                Description = "Rejects a submitted expense. Only managers can do this.",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        expenseId = new { type = "integer", description = "The ID of the expense to reject" },
                        reviewerId = new { type = "integer", description = "The user ID of the manager rejecting" }
                    },
                    required = new[] { "expenseId", "reviewerId" }
                })
            }),
            new ChatCompletionsFunctionToolDefinition(new FunctionDefinition
            {
                Name = "get_dashboard_stats",
                Description = "Gets overall statistics including total expenses, pending approvals, and approved amounts.",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new { }
                })
            })
        };
    }

    private async Task<string> ExecuteFunctionAsync(string functionName, string arguments)
    {
        try
        {
            var args = JsonDocument.Parse(arguments);
            var root = args.RootElement;

            return functionName switch
            {
                "get_expenses" => await ExecuteGetExpenses(root),
                "get_expense_by_id" => await ExecuteGetExpenseById(root),
                "create_expense" => await ExecuteCreateExpense(root),
                "submit_expense" => await ExecuteSubmitExpense(root),
                "get_pending_approvals" => await ExecuteGetPendingApprovals(root),
                "approve_expense" => await ExecuteApproveExpense(root),
                "reject_expense" => await ExecuteRejectExpense(root),
                "get_dashboard_stats" => await ExecuteGetDashboardStats(),
                _ => JsonSerializer.Serialize(new { error = $"Unknown function: {functionName}" })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", functionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private async Task<string> ExecuteGetExpenses(JsonElement args)
    {
        int? userId = args.TryGetProperty("userId", out var u) ? u.GetInt32() : null;
        int? statusId = args.TryGetProperty("statusId", out var s) ? s.GetInt32() : null;
        int? categoryId = args.TryGetProperty("categoryId", out var c) ? c.GetInt32() : null;
        string? searchTerm = args.TryGetProperty("searchTerm", out var t) ? t.GetString() : null;

        var (expenses, _) = await _expenseService.GetExpensesAsync(userId, statusId, categoryId, searchTerm);
        return JsonSerializer.Serialize(expenses);
    }

    private async Task<string> ExecuteGetExpenseById(JsonElement args)
    {
        var expenseId = args.GetProperty("expenseId").GetInt32();
        var (expense, _) = await _expenseService.GetExpenseByIdAsync(expenseId);
        return JsonSerializer.Serialize(expense);
    }

    private async Task<string> ExecuteCreateExpense(JsonElement args)
    {
        var request = new CreateExpenseRequest
        {
            UserId = args.GetProperty("userId").GetInt32(),
            CategoryId = args.GetProperty("categoryId").GetInt32(),
            Amount = args.GetProperty("amount").GetDecimal(),
            ExpenseDate = DateTime.Parse(args.GetProperty("expenseDate").GetString()!),
            Description = args.TryGetProperty("description", out var d) ? d.GetString() : null
        };

        var (expenseId, error) = await _expenseService.CreateExpenseAsync(request);
        return JsonSerializer.Serialize(new { expenseId, error, success = expenseId.HasValue });
    }

    private async Task<string> ExecuteSubmitExpense(JsonElement args)
    {
        var expenseId = args.GetProperty("expenseId").GetInt32();
        var (success, error) = await _expenseService.SubmitExpenseAsync(expenseId);
        return JsonSerializer.Serialize(new { success, error });
    }

    private async Task<string> ExecuteGetPendingApprovals(JsonElement args)
    {
        string? searchTerm = args.TryGetProperty("searchTerm", out var t) ? t.GetString() : null;
        var (expenses, _) = await _expenseService.GetPendingApprovalsAsync(searchTerm);
        return JsonSerializer.Serialize(expenses);
    }

    private async Task<string> ExecuteApproveExpense(JsonElement args)
    {
        var expenseId = args.GetProperty("expenseId").GetInt32();
        var reviewerId = args.GetProperty("reviewerId").GetInt32();
        var (success, error) = await _expenseService.ApproveExpenseAsync(expenseId, reviewerId);
        return JsonSerializer.Serialize(new { success, error });
    }

    private async Task<string> ExecuteRejectExpense(JsonElement args)
    {
        var expenseId = args.GetProperty("expenseId").GetInt32();
        var reviewerId = args.GetProperty("reviewerId").GetInt32();
        var (success, error) = await _expenseService.RejectExpenseAsync(expenseId, reviewerId);
        return JsonSerializer.Serialize(new { success, error });
    }

    private async Task<string> ExecuteGetDashboardStats()
    {
        var (stats, _) = await _expenseService.GetDashboardStatsAsync();
        return JsonSerializer.Serialize(stats);
    }
}
