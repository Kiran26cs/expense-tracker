using Azure.Identity;
using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Middleware;
using ExpensesBackend.API.Services;
using ExpensesBackend.API.Services.Admin;
using ExpensesBackend.API.Services.AI;
using ExpensesBackend.API.Services.BankSync;
using ExpensesBackend.API.Services.Interfaces;
using ExpensesBackend.API.Services.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.Channels;
using ExpensesBackend.API.Infrastructure.Json;

// Check if running migration command
if (args.Length > 0 && args[0] == "migrate")
{
    Console.WriteLine("Running Daily Summaries Migration...");
    var config = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false)
        .Build();
    
    var connectionString = config["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017";
    var databaseName = config["MongoDB:DatabaseName"] ?? "expensesDb";
    
    await ExpensesBackend.API.MigrateDailySummaries.RunMigration(connectionString, databaseName);
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Azure App Configuration — connection string for local dev, endpoint + managed identity for production
var appConfigConnection = builder.Configuration["AzureAppConfig:ConnectionString"];
var appConfigEndpoint = builder.Configuration["AzureAppConfig:Endpoint"];

if (!string.IsNullOrEmpty(appConfigConnection))
{
    // Local dev: authenticate to Key Vault via Azure CLI (requires `az login`)
    builder.Configuration.AddAzureAppConfiguration(options =>
        options.Connect(appConfigConnection)
               .ConfigureKeyVault(kv => kv.SetCredential(new AzureCliCredential())));
}
else if (!string.IsNullOrEmpty(appConfigEndpoint))
{
    // Production: authenticate via Managed Identity assigned to the App Service
    builder.Configuration.AddAzureAppConfiguration(options =>
        options.Connect(new Uri(appConfigEndpoint), new DefaultAzureCredential())
               .ConfigureKeyVault(kv => kv.SetCredential(new DefaultAzureCredential())));
}

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new UtcNullableDateTimeConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MongoDB
builder.Services.AddSingleton<MongoDbContext>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IExpenseBookService, ExpenseBookService>();
builder.Services.AddScoped<IExpenseBookDependencyService, ExpenseBookDependencyService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<ILendingService, LendingService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<IBankConnectionService, BankConnectionService>();
builder.Services.AddScoped<IBankSyncService, BankSyncService>();
builder.Services.AddScoped<AiBankStatementParser>();
builder.Services.AddSingleton<AiBankTransactionCategorizer>();
builder.Services.AddScoped<PdfBankStatementParser>();
builder.Services.AddScoped<BankStatementPdfExtractor>();
builder.Services.AddScoped<BankStatementParserFactory>();
builder.Services.AddHttpClient<ICurrencyConversionService, FrankfurterCurrencyService>();
builder.Services.AddScoped<ITemplateBookService, TemplateBookService>();
builder.Services.AddSingleton<ITemplateBlobService, TemplateBlobService>();
builder.Services.AddMemoryCache();

// Payment
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddHttpClient("Razorpay");

// AI Chat & Credits
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<ICreditService, CreditService>();
builder.Services.AddScoped<SystemPromptBuilder>();
builder.Services.AddScoped<ToolRegistry>();
builder.Services.AddScoped<ClaudeOrchestrator>();
builder.Services.AddScoped<ICategoryClassifier, AiCategoryClassifier>();
builder.Services.AddHttpClient("Claude");

// Bounded channel: at most 50 queued import jobs; back-pressures callers if full
builder.Services.AddSingleton(Channel.CreateBounded<ImportJobPayload>(
    new BoundedChannelOptions(50) { FullMode = BoundedChannelFullMode.Wait }));
builder.Services.AddHostedService<ImportProcessorService>();

// Template creation channel — bounded to 10 concurrent seeding jobs
builder.Services.AddSingleton(Channel.CreateBounded<TemplateCreationJobPayload>(
    new BoundedChannelOptions(10) { FullMode = BoundedChannelFullMode.Wait }));
builder.Services.AddHostedService<TemplateBookProcessorService>();

// Push Notifications
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();
builder.Services.AddHostedService<NotificationSchedulerService>();

// Messaging Service — switch provider via Messaging:Provider in Azure App Configuration
builder.Services.AddHttpClient("MSG91");
var messagingProvider = builder.Configuration["Messaging:Provider"] ?? "MSG91";
// Singleton so the service can be safely captured in fire-and-forget background tasks
// (all implementations are stateless HTTP clients — singleton lifetime is appropriate)
if (messagingProvider.Equals("AzureCommunication", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddSingleton<IMessagingService, AzureCommunicationMessagingService>();
else if (messagingProvider.Equals("TwilioSendGrid", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddSingleton<IMessagingService, TwilioSendGridMessagingService>();
else
    builder.Services.AddSingleton<IMessagingService, Msg91MessagingService>();

// Admin Services
builder.Services.AddScoped<IPlatformAdminAuthService, PlatformAdminAuthService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IAdminCreditService, AdminCreditService>();
builder.Services.AddScoped<IAdminBookService, AdminBookService>();
builder.Services.AddScoped<IAdminCacheService, AdminCacheService>();
builder.Services.AddScoped<IAdminJobService, AdminJobService>();
builder.Services.AddScoped<IAdminPlatformAdminService, AdminPlatformAdminService>();

// Redis Distributed Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
    options.InstanceName = builder.Configuration["Redis:InstanceName"];
});
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "your-super-secret-key-min-32-chars-long";
var key = Encoding.UTF8.GetBytes(jwtSecret);
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ExpensesBackend";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "ExpensesBackend",
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
})
.AddJwtBearer("AdminBearer", options =>
{
    // Separate scheme for platform admins — different audience prevents cross-use of tokens
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = "platform-admin-v1",
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("PlatformAdmin", policy =>
    {
        policy.AddAuthenticationSchemes("AdminBearer");
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("type", "platform_admin");
    });

// CORS — origins from config + local dev defaults
var frontendUrl = builder.Configuration["App:FrontendUrl"];
var corsOrigins = new List<string> { "http://localhost:4200", "http://localhost:5173" };
if (!string.IsNullOrEmpty(frontendUrl))
    corsOrigins.Add(frontendUrl.TrimEnd('/'));
// Always allow the production domains
corsOrigins.Add("https://nidhiwise.com");
corsOrigins.Add("https://app.nidhiwise.com");
corsOrigins.Add("https://admin.nidhiwise.com");
corsOrigins.Add("http://localhost:4300"); // admin app local dev

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins([.. corsOrigins])
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithExposedHeaders("X-Api-Version");
    });
});

// Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ApiVersionMiddleware>();

app.MapControllers();

app.Run();
