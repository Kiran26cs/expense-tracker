# Expense Tracker - Frontend & Backend Integration Guide

## Overview
This guide will help you run and integrate the frontend (React + TypeScript) with the backend (.NET 8 API) for the Expense Tracker application.

## Prerequisites

### Backend Requirements
- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **MongoDB** - [Download](https://www.mongodb.com/try/download/community) or use MongoDB Atlas (cloud)
- **MongoDB Compass** (optional) - For database management

### Frontend Requirements
- **Node.js** (v18 or higher) - [Download](https://nodejs.org/)
- **npm** or **yarn** package manager

## Quick Start

### 1. Setup MongoDB

#### Option A: Local MongoDB
1. Install MongoDB Community Edition
2. Start MongoDB service:
   ```powershell
   # Windows - MongoDB should start automatically as a service
   # Or manually start it:
   mongod --dbpath "C:\data\db"
   ```

#### Option B: MongoDB Atlas (Cloud)
1. Create a free account at [MongoDB Atlas](https://www.mongodb.com/cloud/atlas)
2. Create a cluster and get your connection string
3. Update `expensesBackend/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "MongoDB": "your-mongodb-atlas-connection-string"
   }
   ```

### 2. Start the Backend API

Open a terminal in the `expensesBackend` folder:

```powershell
# Navigate to backend folder
cd expensesBackend

# Restore dependencies (first time only)
dotnet restore

# Run the backend
dotnet run
```

The backend API will start at:
- **HTTP**: http://localhost:5196
- **HTTPS**: https://localhost:7250
- **Swagger UI**: http://localhost:5196/swagger

### 3. Start the Frontend

Open a **new terminal** in the `webapps` folder:

```powershell
# Navigate to frontend folder
cd webapps

# Install dependencies (first time only)
npm install

# Start the development server
npm run dev
```

The frontend will start at:
- **URL**: http://localhost:3000

## Configuration Details

### Backend Configuration (expensesBackend/appsettings.json)
```json
{
  "ConnectionStrings": {
    "MongoDB": "mongodb://localhost:27017"
  },
  "MongoDB": {
    "DatabaseName": "ExpenseTrackerDB"
  },
  "Jwt": {
    "Secret": "your-super-secret-key-min-32-chars-long-for-production-change-this",
    "Issuer": "ExpensesBackend",
    "Audience": "ExpensesBackend",
    "ExpirationHours": 24
  }
}
```

### Frontend Configuration (webapps/.env)
```env
VITE_API_BASE_URL=http://localhost:5196/api
```

### CORS Configuration
The backend is configured to accept requests from:
- http://localhost:3000 (React dev server)
- http://localhost:5173 (Alternative Vite port)

## Testing the Integration

### 1. Access the Application
Open your browser and navigate to http://localhost:3000

### 2. Test Authentication Flow
1. Click "Sign Up" or "Sign In"
2. Enter your email and/or phone number
3. Request OTP code
4. Verify OTP
5. Complete registration or login

### 3. Test Features
- **Dashboard**: View expense overview and charts
- **Add Expense**: Create new expense entries
- **Budget**: Set and track budgets by category
- **Insights**: View spending analytics and trends
- **Settings**: Update profile and preferences

## API Endpoints

The backend exposes the following endpoints:

### Authentication
- `POST /api/Auth/send-otp` - Send OTP for login/signup
- `POST /api/Auth/verify-otp` - Verify OTP code
- `POST /api/Auth/signup` - Register new user
- `POST /api/Auth/login` - Login existing user

### Expenses
- `GET /api/Expenses` - Get all expenses
- `GET /api/Expenses/{id}` - Get expense by ID
- `POST /api/Expenses` - Create new expense
- `PUT /api/Expenses/{id}` - Update expense
- `DELETE /api/Expenses/{id}` - Delete expense
- `GET /api/Expenses/category/{category}` - Get expenses by category

### Dashboard
- `GET /api/Dashboard/summary` - Get dashboard summary
- `GET /api/Dashboard/monthly-trend` - Get monthly spending trend
- `GET /api/Dashboard/category-breakdown` - Get category-wise breakdown

## Troubleshooting

### Backend Issues

**Problem**: "Unable to connect to MongoDB"
```
Solution: 
1. Ensure MongoDB service is running
2. Check connection string in appsettings.json
3. Verify MongoDB is listening on port 27017
```

**Problem**: "Port 5196 is already in use"
```
Solution:
1. Find and kill the process using that port:
   Get-Process -Id (Get-NetTCPConnection -LocalPort 5196).OwningProcess | Stop-Process -Force
2. Or change the port in launchSettings.json
```

**Problem**: CORS errors in browser
```
Solution:
1. Verify backend is running
2. Check CORS configuration in Program.cs
3. Ensure frontend URL matches CORS policy
```

### Frontend Issues

**Problem**: "Failed to fetch" or network errors
```
Solution:
1. Verify backend is running on port 5196
2. Check .env file has correct API URL
3. Restart the frontend dev server after .env changes
```

**Problem**: "Port 3000 is already in use"
```
Solution:
1. Kill the process using port 3000
2. Or change port in vite.config.ts
3. Update CORS policy in backend if using different port
```

**Problem**: Authentication token issues
```
Solution:
1. Clear localStorage in browser DevTools
2. Logout and login again
3. Check JWT configuration in backend
```

## Development Workflow

### Making Backend Changes
1. Edit code in `expensesBackend/` folder
2. Backend will hot-reload automatically with `dotnet watch run`
3. Or stop and restart with `dotnet run`

### Making Frontend Changes
1. Edit code in `webapps/src/` folder
2. Vite will hot-reload automatically
3. Changes appear instantly in browser

## Database Management

### View Data with MongoDB Compass
1. Open MongoDB Compass
2. Connect to: `mongodb://localhost:27017`
3. Database: `ExpenseTrackerDB`
4. Collections: `users`, `expenses`, `budgets`, `categories`, `recurringExpenses`

### Reset Database
```powershell
# Connect to MongoDB
mongosh

# Use database
use ExpenseTrackerDB

# Drop all collections
db.users.drop()
db.expenses.drop()
db.budgets.drop()
```

## Production Deployment

### Backend
1. Update `appsettings.Production.json` with production values
2. Set strong JWT secret
3. Use production MongoDB connection string
4. Build: `dotnet publish -c Release`
5. Deploy to Azure, AWS, or your hosting provider

### Frontend
1. Update `.env.production` with production API URL
2. Build: `npm run build`
3. Deploy `dist/` folder to Netlify, Vercel, or static hosting

## Security Notes

⚠️ **Important**: Before deploying to production:
1. Change the JWT secret in `appsettings.json`
2. Use environment variables for sensitive data
3. Enable HTTPS in production
4. Update CORS policy for production domain
5. Use strong MongoDB credentials

## Support

For issues or questions:
1. Check API documentation at http://localhost:5196/swagger
2. Review logs in terminal/console
3. Check browser DevTools Network tab for API calls
4. Verify MongoDB connection and data

## Additional Resources

- [Backend API Examples](expensesBackend/API_EXAMPLES.md)
- [Frontend Documentation](webapps/DOCUMENTATION.md)
- [Backend Quick Start](expensesBackend/QUICKSTART.md)
- [Frontend Quick Start](webapps/QUICKSTART.md)
