# Expense Tracker - Quick Start Checklist

## Before First Run

### 1. MongoDB Setup
- [ ] MongoDB is installed
- [ ] MongoDB service is running
- [ ] Database connection string is correct in `expensesBackend/appsettings.json`

### 2. Backend Setup
- [ ] .NET 8 SDK is installed
- [ ] Navigate to `expensesBackend` folder
- [ ] Run: `dotnet restore`
- [ ] Verify `appsettings.json` configuration

### 3. Frontend Setup
- [ ] Node.js (v18+) is installed
- [ ] Navigate to `webapps` folder
- [ ] Run: `npm install`
- [ ] Verify `.env` file exists with correct API URL

## Running the Application

### Option 1: Using the Startup Script (Recommended)
```powershell
# From the expenseTracker folder
.\start.ps1
```

### Option 2: Manual Start

#### Terminal 1 - Backend
```powershell
cd expensesBackend
dotnet run
```
✅ Backend should start at http://localhost:5196

#### Terminal 2 - Frontend
```powershell
cd webapps
npm run dev
```
✅ Frontend should start at http://localhost:3000

## Verification Steps

### 1. Check Backend is Running
- [ ] Open http://localhost:5196/swagger
- [ ] You should see the API documentation
- [ ] All endpoints should be listed

### 2. Check Frontend is Running
- [ ] Open http://localhost:3000
- [ ] You should see the login/signup page
- [ ] Page should load without errors

### 3. Test Integration
- [ ] Try to sign up with email/phone
- [ ] Verify OTP functionality works
- [ ] Login to the application
- [ ] Try adding an expense
- [ ] View dashboard

## Common Issues & Solutions

### MongoDB Not Running
```
Error: Unable to connect to MongoDB
Solution: Start MongoDB service
```

### Port Already in Use
```
Backend (5196): Stop the existing process
Frontend (3000): Stop the existing process or change port in vite.config.ts
```

### CORS Errors
```
Solution: Ensure backend is running and CORS policy includes your frontend URL
```

### API Connection Failed
```
1. Check backend is running
2. Verify .env file has correct API URL
3. Clear browser cache and restart frontend
```

## URLs Quick Reference

- **Frontend**: http://localhost:3000
- **Backend API**: http://localhost:5196
- **Swagger Docs**: http://localhost:5196/swagger
- **MongoDB**: mongodb://localhost:27017

## Next Steps

Once everything is running:
1. Create an account using the signup flow
2. Add your first expense
3. Set up budgets for different categories
4. Explore the dashboard and insights
5. Check out recurring expenses feature

## Need Help?

Refer to the detailed [INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md) for:
- Detailed setup instructions
- Troubleshooting guide
- API documentation
- Development workflow
- Production deployment tips
