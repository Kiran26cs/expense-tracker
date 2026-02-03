# ✅ Pre-Launch Checklist

Use this checklist to ensure everything is ready before running your Expense Tracker.

## 🔧 Prerequisites Installation

### MongoDB
- [ ] Downloaded from https://www.mongodb.com/try/download/community
- [ ] Installed successfully
- [ ] Service can be started with `mongod --dbpath "C:\data\db"`
- [ ] Verified: `mongosh` connects successfully

### .NET 8 SDK
- [ ] Downloaded from https://dotnet.microsoft.com/download/dotnet/8.0
- [ ] Installed successfully
- [ ] Verified: `dotnet --version` shows 8.x.x
- [ ] Verified: Can run `dotnet restore` and `dotnet run`

### Node.js
- [ ] Downloaded from https://nodejs.org/
- [ ] Installed version 18 or higher
- [ ] Verified: `node --version` shows v18+
- [ ] Verified: `npm --version` shows version
- [ ] Verified: `npm install` works in webapps folder

## 📝 Configuration Files

### Backend Configuration
- [ ] `expensesBackend/appsettings.json` exists
- [ ] MongoDB connection string configured correctly
- [ ] JWT secret configured (change in production!)
- [ ] CORS policy allows localhost:3000
- [ ] Program.cs has JWT authentication enabled

### Frontend Configuration
- [ ] `webapps/.env` file exists
- [ ] `VITE_API_BASE_URL=http://localhost:5196/api` is set
- [ ] `.env.development` file exists
- [ ] `vite.config.ts` has proxy configured
- [ ] `tsconfig.json` is valid

## 🗂️ Project Structure

### Backend Structure
- [ ] Controllers folder exists with AuthController, ExpensesController, DashboardController
- [ ] Services folder exists with service implementations
- [ ] Domain folder exists with Entities and DTOs
- [ ] Infrastructure/Data folder with MongoDbContext
- [ ] Program.cs configures all services

### Frontend Structure
- [ ] src/pages folder with Auth, Dashboard, Expenses, Budget, Insights
- [ ] src/components folder with UI components
- [ ] src/services folder with API services
- [ ] src/hooks folder with custom hooks
- [ ] src/types folder with TypeScript definitions
- [ ] src/styles folder with CSS files
- [ ] package.json has required dependencies

## 🔗 API Endpoint Configuration

### Authentication Endpoints
- [ ] `/Auth/send-otp` - Frontend calls with correct endpoint
- [ ] `/Auth/verify-otp` - Frontend calls with correct endpoint
- [ ] `/Auth/signup` - Frontend calls with correct endpoint
- [ ] `/Auth/login` - Frontend calls with correct endpoint

### Expense Endpoints
- [ ] `/Expenses` - Capitalized in frontend
- [ ] `/Expenses/{id}` - Capitalized in frontend
- [ ] `/Expenses/recurring` - Capitalized in frontend

### Dashboard Endpoints
- [ ] `/Dashboard` - Capitalized in frontend
- [ ] `/Dashboard/summary` - Available in backend

## 🚀 Startup Scripts

- [ ] `start.ps1` exists and is executable
- [ ] `start.bat` exists and is executable
- [ ] Scripts have correct paths
- [ ] Both MongoDB, backend, and frontend commands are included

## 📚 Documentation

- [ ] `README.md` - Main overview
- [ ] `00_START_HERE.md` - Entry point
- [ ] `COMMANDS.md` - CLI commands
- [ ] `INTEGRATION_GUIDE.md` - Detailed guide
- [ ] `ARCHITECTURE.md` - System design
- [ ] `VISUAL_GUIDE.md` - Diagrams and flows
- [ ] `INDEX.md` - Documentation index
- [ ] `API_MAPPING.md` - API reference

## 🧪 Pre-Launch Tests

### Environment Variables
- [ ] Run: `echo %VITE_API_BASE_URL%` in webapps folder (Windows)
- [ ] Should show: `http://localhost:5196/api`

### Backend Startup Test
- [ ] Navigate to expensesBackend folder
- [ ] Run: `dotnet restore` (should complete without errors)
- [ ] Run: `dotnet run` (should start and listen on 5196)
- [ ] Open: http://localhost:5196/swagger (should show API docs)
- [ ] Stop with: Ctrl+C

### Frontend Startup Test
- [ ] Navigate to webapps folder
- [ ] Run: `npm install` (should install all packages)
- [ ] Run: `npm run dev` (should start on port 3000)
- [ ] Open: http://localhost:3000 (should show login page)
- [ ] Stop with: Ctrl+C

### Database Connection Test
```powershell
mongosh
use ExpenseTrackerDB
show collections
exit
```
- [ ] MongoDB connects successfully
- [ ] Can switch to ExpenseTrackerDB database
- [ ] Database exists (should show no collections initially)

## 🔐 Security Checks

- [ ] JWT secret is set in appsettings.json
- [ ] CORS policy includes http://localhost:3000
- [ ] Tokens are stored in localStorage (frontend)
- [ ] Authorization headers are sent with requests
- [ ] Password hashing is implemented in AuthService

## 🎯 Feature Readiness

### Core Features
- [ ] Authentication (OTP-based)
- [ ] Expense CRUD operations
- [ ] Dashboard summary
- [ ] Category filtering
- [ ] Recurring expenses

### Advanced Features
- [ ] Budget tracking (backend controller needed)
- [ ] Expense insights
- [ ] Monthly trends
- [ ] Spending forecasts (service needed)

## 📊 Data Preparation

### MongoDB Collections
- [ ] Collections will be auto-created on first data insert
- [ ] Or manually create if needed:
  - users
  - expenses
  - budgets
  - categories
  - recurringExpenses

### Indexes (Optional)
- [ ] Consider adding indexes for:
  - User queries (email, phone)
  - Expense queries (userId, date)
  - Budget queries (userId, category)

## 🚨 Potential Issues to Watch

### Issue: MongoDB Not Running
- [ ] Verify MongoDB service is started
- [ ] Check: `Get-Process mongod` (Windows)
- [ ] Fix: `mongod --dbpath "C:\data\db"`

### Issue: Port Already in Use
- [ ] Backend port 5196: Find and stop the process
- [ ] Frontend port 3000: Find and stop the process
- [ ] Or change ports and update configuration

### Issue: CORS Errors in Browser
- [ ] Verify backend is running
- [ ] Check CORS policy in Program.cs
- [ ] Verify frontend URL matches CORS allowed origins

### Issue: API Not Responding
- [ ] Check backend is running on correct port
- [ ] Verify .env file has correct API URL
- [ ] Check browser Network tab for actual request URLs
- [ ] Restart both frontend and backend

### Issue: Module Not Found
- [ ] Backend: Run `dotnet restore` in expensesBackend
- [ ] Frontend: Run `npm install` in webapps

## ✅ Final Verification

Before declaring ready:

```powershell
# 1. Check all software
dotnet --version     # Should show 8.x.x
node --version       # Should show v18+
npm --version        # Should show version number
mongosh --version    # Should show version number

# 2. Navigate to project
cd d:\flutterRepo\expenseTracker

# 3. Check all files exist
test -f .\start.ps1                          # PowerShell
test -f .\start.bat                          # Batch
test -f .\README.md                          # Documentation
test -f .\webapps\.env                       # Frontend config
test -f .\expensesBackend\appsettings.json  # Backend config

# 4. Run startup script
.\start.ps1

# 5. Verify services
# Backend: http://localhost:5196/swagger
# Frontend: http://localhost:3000
# MongoDB: mongosh connection
```

## 🎉 Launch Readiness

- [ ] All prerequisites installed ✓
- [ ] All configuration files in place ✓
- [ ] All documentation available ✓
- [ ] All startup scripts ready ✓
- [ ] All endpoints aligned ✓
- [ ] Pre-launch tests passed ✓
- [ ] No issues identified ✓

## 🚀 Ready to Launch!

Once all checkboxes above are checked:

```powershell
# Start MongoDB
mongod --dbpath "C:\data\db"

# Start application
cd d:\flutterRepo\expenseTracker
.\start.ps1
```

Open browser: http://localhost:3000

**Your Expense Tracker is running!** 🎉

---

## 📞 If Something Goes Wrong

1. Stop all services (Ctrl+C)
2. Refer to `INTEGRATION_GUIDE.md` troubleshooting section
3. Check logs in terminal windows
4. Verify prerequisites are installed correctly
5. Check all configuration files are present and correct
6. Restart everything and try again

---

**Checklist Version: 1.0**
**Last Updated: February 2, 2026**

**Once you've completed all items above, your application is ready to launch!**
