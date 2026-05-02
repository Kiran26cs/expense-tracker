using Azure.Identity;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Middleware;
using ExpensesBackend.API.Services;
using ExpensesBackend.API.Services.Interfaces;
using ExpensesBackend.API.Services.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

// Messaging Service — switch provider via Messaging:Provider in appsettings.json
builder.Services.AddHttpClient("MSG91");
var messagingProvider = builder.Configuration["Messaging:Provider"] ?? "MSG91";
if (messagingProvider.Equals("TwilioSendGrid", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddScoped<IMessagingService, TwilioSendGridMessagingService>();
else
    builder.Services.AddScoped<IMessagingService, Msg91MessagingService>();

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
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "ExpensesBackend",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "ExpensesBackend",
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://localhost:3001", "http://localhost:3002", "http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
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

app.MapControllers();

app.Run();
