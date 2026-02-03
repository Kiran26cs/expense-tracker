# 🚀 Quick Start Commands

## Option 1: Automatic (Recommended)

### Windows - PowerShell
```powershell
# Navigate to expenseTracker folder
cd d:\flutterRepo\expenseTracker

# Make sure MongoDB is running first
mongod --dbpath "C:\data\db"

# Run the startup script
.\start.ps1
```

### Windows - Command Prompt
```cmd
cd d:\flutterRepo\expenseTracker
start.bat
```

## Option 2: Manual (Two Terminals)

### Terminal 1 - Start Backend
```powershell
cd d:\flutterRepo\expenseTracker\expensesBackend
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5196
```

### Terminal 2 - Start Frontend
```powershell
cd d:\flutterRepo\expenseTracker\webapps
npm install
npm run dev
```

Expected output:
```
VITE v... ready in ... ms

➜  Local:   http://localhost:3000/
```

## Step 3: Access Application

Open your browser and go to:
```
http://localhost:3000
```

You should see the Expense Tracker login/signup page.

## Verify Integration

### Check Backend
Open in browser:
```
http://localhost:5196/swagger
```
You should see API documentation with all endpoints listed.

### Check Frontend
Browser DevTools (F12) → Network tab:
1. Go to http://localhost:3000
2. Try any API call (login, signup, etc.)
3. You should see requests to `http://localhost:5196/api/...`

### Check MongoDB
```powershell
# Open MongoDB shell
mongosh

# View databases
show databases

# Use expense tracker database
use ExpenseTrackerDB

# View collections
show collections

# View users
db.users.find()
```

## First Time Setup

### Install Dependencies (One Time Only)

```powershell
# Backend
cd expensesBackend
dotnet restore

# Frontend
cd ..\webapps
npm install
```

## Environment Files

Your application has these configuration files:

**Backend:** `expensesBackend/appsettings.json`
```json
{
  "ConnectionStrings": {
    "MongoDB": "mongodb://localhost:27017"
  },
  "MongoDB": {
    "DatabaseName": "ExpenseTrackerDB"
  }
}
```

**Frontend:** `webapps/.env`
```env
VITE_API_BASE_URL=http://localhost:5196/api
```

## API Ports Reference

```
Frontend:   http://localhost:3000
Backend:    http://localhost:5196
Swagger:    http://localhost:5196/swagger
MongoDB:    mongodb://localhost:27017
```

## Stop Services

### PowerShell Script / Batch File
- Close both terminal windows

### Manual Terminal
- Terminal 1: Press `Ctrl+C`
- Terminal 2: Press `Ctrl+C`

## Troubleshooting Quick Fixes

### MongoDB Not Running
```powershell
# Start MongoDB
mongod --dbpath "C:\data\db"
```

### Port 5196 In Use
```powershell
# Kill process using port 5196
Get-Process -Id (Get-NetTCPConnection -LocalPort 5196).OwningProcess | Stop-Process -Force
```

### Port 3000 In Use
```powershell
# Kill process using port 3000
Get-Process -Id (Get-NetTCPConnection -LocalPort 3000).OwningProcess | Stop-Process -Force
```

### NPM Packages Not Found
```powershell
cd webapps
npm install
npm run dev
```

### .NET Dependencies Not Found
```powershell
cd expensesBackend
dotnet restore
dotnet run
```

## Testing the Integration

1. **Open Frontend**: http://localhost:3000
2. **Sign Up**
   - Enter email
   - Click "Send OTP"
   - Enter OTP (use any 6 digits for testing if SMS not configured)
   - Complete signup

3. **Login**
   - Use your credentials
   - Should redirect to dashboard

4. **Add Expense**
   - Click "Add Expense"
   - Fill in details
   - Submit
   - Check if it appears in the list

5. **View Dashboard**
   - Should show summary
   - Monthly charts
   - Category breakdown

## Production Deployment

When ready to deploy:

```powershell
# Backend
cd expensesBackend
dotnet publish -c Release
# Deploy the publish folder to your hosting

# Frontend
cd webapps
npm run build
# Deploy the dist folder to your hosting
```

---

**Need Help?** See [INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md) for detailed troubleshooting.
