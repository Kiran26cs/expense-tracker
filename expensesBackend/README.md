# Expenses Backend API

A .NET 8 Web API backend for the personal expense tracker application with MongoDB and clean architecture.

## Features

- **JWT Authentication with OTP**: Email/phone-based OTP authentication
- **Expense Management**: Full CRUD operations for expenses with receipt upload
- **Dashboard Analytics**: Real-time expense summaries and category breakdowns
- **Budget Planning**: Budget tracking and alerting system
- **Recurring Expenses**: Automated recurring expense management
- **MongoDB Integration**: NoSQL database with optimized indexes
- **Clean Architecture**: Separation of concerns with Domain, Services, and Infrastructure layers
- **Global Exception Handling**: Consistent error responses
- **CORS Support**: Configured for frontend integration
- **Swagger Documentation**: Interactive API documentation

## Tech Stack

- **.NET 8**: Latest LTS version
- **MongoDB Driver 3.6**: NoSQL database integration
- **JWT Bearer Authentication**: Secure token-based auth
- **FluentValidation**: Request validation
- **Swashbuckle**: API documentation

## Project Structure

```
ExpensesBackend/
├── Controllers/          # API endpoints
│   ├── AuthController.cs
│   ├── ExpensesController.cs
│   └── DashboardController.cs
├── Domain/
│   ├── Entities/        # MongoDB models
│   │   ├── User.cs
│   │   ├── Expense.cs
│   │   ├── Category.cs
│   │   ├── Budget.cs
│   │   └── RecurringExpense.cs
│   └── DTOs/            # Data transfer objects
│       ├── ApiResponse.cs
│       ├── AuthDTOs.cs
│       ├── ExpenseDTOs.cs
│       └── DashboardDTOs.cs
├── Services/            # Business logic
│   ├── Interfaces/
│   │   ├── IAuthService.cs
│   │   ├── IExpenseService.cs
│   │   └── IDashboardService.cs
│   ├── AuthService.cs
│   ├── ExpenseService.cs
│   └── DashboardService.cs
├── Infrastructure/      # Data access
│   └── Data/
│       └── MongoDbContext.cs
├── Middleware/          # Custom middleware
│   └── GlobalExceptionHandler.cs
├── Program.cs           # Application entry point
└── appsettings.json     # Configuration
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [MongoDB](https://www.mongodb.com/try/download/community) (running on localhost:27017)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

## Getting Started

### 1. Install MongoDB

**Windows:**
```powershell
# Download and install from https://www.mongodb.com/try/download/community
# Or use Chocolatey
choco install mongodb

# Start MongoDB service
net start MongoDB
```

**macOS:**
```bash
brew tap mongodb/brew
brew install mongodb-community
brew services start mongodb-community
```

### 2. Configure Application

Update [appsettings.json](appsettings.json) with your MongoDB connection string:

```json
{
  "ConnectionStrings": {
    "MongoDB": "mongodb://localhost:27017"
  },
  "MongoDB": {
    "DatabaseName": "ExpenseTrackerDB"
  },
  "Jwt": {
    "Secret": "CHANGE-THIS-TO-A-SECURE-SECRET-KEY-MIN-32-CHARS",
    "Issuer": "ExpensesBackend",
    "Audience": "ExpensesBackend",
    "ExpirationHours": 24
  }
}
```

### 3. Restore Dependencies

```powershell
dotnet restore
```

### 4. Build the Project

```powershell
dotnet build
```

### 5. Run the Application

```powershell
dotnet run
```

The API will be available at:
- HTTPS: `https://localhost:7001`
- HTTP: `http://localhost:5001`
- Swagger UI: `https://localhost:7001/swagger`

## API Endpoints

### Authentication

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/send-otp` | Send OTP to email/phone |
| POST | `/api/auth/verify-otp` | Verify OTP code |
| POST | `/api/auth/signup` | Register new user |
| POST | `/api/auth/login` | Login with OTP |
| GET | `/api/auth/me` | Get current user (requires auth) |

### Expenses

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/expenses` | Get all expenses (with filters) |
| GET | `/api/expenses/{id}` | Get expense by ID |
| POST | `/api/expenses` | Create new expense |
| PUT | `/api/expenses/{id}` | Update expense |
| DELETE | `/api/expenses/{id}` | Delete expense |
| POST | `/api/expenses/{id}/receipt` | Upload receipt |

### Dashboard

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/dashboard/summary` | Get dashboard summary |
| GET | `/api/dashboard/trends` | Get monthly trends |

## API Response Format

All endpoints return responses in the following format:

```json
{
  "success": true,
  "data": { /* response data */ },
  "error": null
}
```

Error responses:

```json
{
  "success": false,
  "data": null,
  "error": "Error message"
}
```

## Authentication Flow

1. **Send OTP**: User provides email or phone
   ```json
   POST /api/auth/send-otp
   {
     "email": "user@example.com"
   }
   ```

2. **Verify OTP**: User enters received OTP
   ```json
   POST /api/auth/verify-otp
   {
     "email": "user@example.com",
     "otp": "123456"
   }
   ```

3. **Login/Signup**: Complete authentication
   ```json
   POST /api/auth/login?otp=123456
   {
     "email": "user@example.com"
   }
   ```

4. **Use Token**: Include in Authorization header
   ```
   Authorization: Bearer <jwt-token>
   ```

## Database Collections

- **users**: User accounts and preferences
- **expenses**: Individual expense transactions
- **categories**: Expense categories (income/expense)
- **budgets**: Budget limits and tracking
- **recurringExpenses**: Recurring expense schedules

## Development

### Adding New Endpoints

1. Create DTOs in `Domain/DTOs/`
2. Add service interface in `Services/Interfaces/`
3. Implement service in `Services/`
4. Create controller in `Controllers/`
5. Register service in `Program.cs`

### Running Tests

```powershell
dotnet test
```

### Code Style

- Follow C# coding conventions
- Use async/await for async operations
- Implement proper error handling
- Add XML documentation comments

## Deployment

### Docker

Create `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ExpensesBackend.API.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ExpensesBackend.API.dll"]
```

Build and run:

```powershell
docker build -t expenses-backend .
docker run -p 5001:80 expenses-backend
```

### Environment Variables

For production, use environment variables instead of appsettings.json:

```bash
ConnectionStrings__MongoDB=mongodb://production-server:27017
MongoDB__DatabaseName=ExpenseTrackerDB
Jwt__Secret=your-production-secret-key
```

## Security Considerations

- **Change JWT Secret**: Use a strong, unique secret key in production
- **HTTPS Only**: Enforce HTTPS in production
- **OTP Implementation**: Replace console logging with actual email/SMS service
- **Rate Limiting**: Implement rate limiting for OTP endpoints
- **File Upload**: Implement proper file validation and storage
- **Connection Strings**: Store sensitive data in Azure Key Vault or similar

## Frontend Integration

This backend is designed to work with the React frontend available at `../expensesBackend` (parent directory).

Update frontend API base URL to:
```typescript
const API_BASE_URL = 'https://localhost:7001/api';
```

## Troubleshooting

### MongoDB Connection Issues

```powershell
# Check if MongoDB is running
Get-Service MongoDB

# Start MongoDB
net start MongoDB
```

### Build Errors

```powershell
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

### Port Already in Use

Update ports in [Properties/launchSettings.json](Properties/launchSettings.json):

```json
{
  "applicationUrl": "https://localhost:7002;http://localhost:5002"
}
```

## License

MIT License

## Support

For issues and questions, please create an issue in the repository.
