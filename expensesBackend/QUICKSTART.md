# Quick Start Guide - Expenses Backend API

## Prerequisites Check

✅ .NET 8 SDK installed  
✅ MongoDB installed and running  
✅ VS Code or Visual Studio 2022

## 5-Minute Setup

### Step 1: Start MongoDB

```powershell
# Windows - Start MongoDB service
net start MongoDB

# Or check if already running
Get-Service MongoDB
```

### Step 2: Update Configuration (Optional)

Edit [appsettings.json](appsettings.json) if needed:

```json
{
  "ConnectionStrings": {
    "MongoDB": "mongodb://localhost:27017"
  },
  "Jwt": {
    "Secret": "your-super-secret-key-min-32-chars-long-for-production-change-this"
  }
}
```

### Step 3: Run the Application

```powershell
# In VS Code terminal
dotnet run

# Or use the VS Code task (Ctrl+Shift+P -> "Tasks: Run Task" -> "Run API")
```

The API will start at:
- **Swagger UI**: http://localhost:5196/swagger
- **API Base**: http://localhost:5196/api

### Step 4: Test the API

#### Option 1: Using Swagger UI

1. Open http://localhost:5196/swagger in your browser
2. Try the `/api/auth/send-otp` endpoint
3. Check the console output for the OTP code

#### Option 2: Using PowerShell

```powershell
# Send OTP
$response = Invoke-RestMethod -Uri "http://localhost:5196/api/auth/send-otp" `
    -Method POST `
    -ContentType "application/json" `
    -Body '{"email":"test@example.com"}'

# Check console output for OTP, then login
$loginResponse = Invoke-RestMethod -Uri "http://localhost:5196/api/auth/login?otp=123456" `
    -Method POST `
    -ContentType "application/json" `
    -Body '{"email":"test@example.com"}'

# Save token
$token = $loginResponse.data.token

# Get dashboard summary
$headers = @{
    "Authorization" = "Bearer $token"
}
$summary = Invoke-RestMethod -Uri "http://localhost:5196/api/dashboard/summary" `
    -Method GET `
    -Headers $headers
```

## Project Structure Overview

```
ExpensesBackend/
├── Controllers/        → API endpoints
├── Domain/
│   ├── Entities/      → MongoDB models
│   └── DTOs/          → Request/Response objects
├── Services/          → Business logic
├── Infrastructure/    → Database access
├── Middleware/        → Exception handling
└── Program.cs         → App configuration
```

## Key API Endpoints

### Authentication
- `POST /api/auth/send-otp` - Send OTP code
- `POST /api/auth/login` - Login with OTP
- `POST /api/auth/signup` - Register new user

### Expenses
- `GET /api/expenses` - List all expenses
- `POST /api/expenses` - Create expense
- `PUT /api/expenses/{id}` - Update expense
- `DELETE /api/expenses/{id}` - Delete expense

### Dashboard
- `GET /api/dashboard/summary` - Get analytics
- `GET /api/dashboard/trends` - Get monthly trends

## Common Tasks

### View Logs

The application logs appear in the terminal. Look for:
- OTP codes (printed to console)
- HTTP requests
- Error messages

### Restart the API

```powershell
# Press Ctrl+C to stop, then run again
dotnet run
```

### Clear Build Cache

```powershell
dotnet clean
dotnet restore
dotnet build
```

## Frontend Integration

The React frontend expects the API at:
```
http://localhost:5196/api
```

Update the frontend `.env` file:
```env
VITE_API_BASE_URL=http://localhost:5196/api
```

## MongoDB Management

### View Data

```powershell
# Connect to MongoDB shell
mongosh

# Use database
use ExpenseTrackerDB

# View collections
show collections

# Query users
db.users.find().pretty()

# Query expenses
db.expenses.find().pretty()
```

### Reset Database

```powershell
# In mongosh
use ExpenseTrackerDB
db.dropDatabase()
```

## Troubleshooting

### MongoDB Not Running

```powershell
# Start MongoDB
net start MongoDB

# Or install if missing
# Download from: https://www.mongodb.com/try/download/community
```

### Port Already in Use

Edit [Properties/launchSettings.json](Properties/launchSettings.json) to change ports.

### Build Errors

```powershell
# Clean and restore
dotnet clean
dotnet restore
dotnet build
```

### OTP Not Appearing

Check the **console output** where you ran `dotnet run`. The OTP will be printed there (for development).

## Next Steps

1. ✅ API is running successfully
2. 📱 Set up the React frontend
3. 🔐 Configure real email/SMS for OTP in production
4. 🗄️ Set up MongoDB Atlas for cloud hosting
5. 🚀 Deploy to Azure/AWS

## Development Workflow

1. Make code changes
2. Application auto-reloads (hot reload)
3. Test in Swagger UI or frontend
4. Check logs in terminal
5. Commit changes to Git

## Production Deployment

For production deployment:

1. Update `appsettings.json` with production MongoDB URI
2. Change JWT secret to a secure value
3. Enable HTTPS only
4. Implement real email/SMS OTP service
5. Add rate limiting
6. Set up monitoring and logging

See [README.md](README.md) for detailed deployment instructions.

## Need Help?

- 📖 Full documentation: [README.md](README.md)
- 🐛 Issues: Check console logs and MongoDB connection
- 💡 Swagger UI: http://localhost:5196/swagger

Happy coding! 🎉
