# 📚 Expense Tracker - Complete Documentation Index

## 🎯 Start Here

**New to the project?** Follow this path:

1. 📖 Read: [README.md](README.md) (5 min overview)
2. ⚡ Run: [COMMANDS.md](COMMANDS.md) (quick start commands)
3. ✅ Checklist: [QUICKSTART_CHECKLIST.md](QUICKSTART_CHECKLIST.md) (verification)
4. 🔧 Deep Dive: [INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md) (detailed setup)

---

## 📄 Documentation Files

### Quick Reference
| Document | Purpose | Read Time |
|----------|---------|-----------|
| [README.md](README.md) | Project overview & quick start | 5 min |
| [COMMANDS.md](COMMANDS.md) | All commands to run the app | 3 min |
| [QUICKSTART_CHECKLIST.md](QUICKSTART_CHECKLIST.md) | Step-by-step verification | 2 min |

### Detailed Guides
| Document | Purpose | Read Time |
|----------|---------|-----------|
| [INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md) | Complete setup & troubleshooting | 15 min |
| [INTEGRATION_STATUS.md](INTEGRATION_STATUS.md) | What was done & current status | 10 min |
| [ARCHITECTURE.md](ARCHITECTURE.md) | System design & data flow | 10 min |
| [API_MAPPING.md](API_MAPPING.md) | API endpoint reference | 5 min |

### Backend Documentation
| Location | Purpose |
|----------|---------|
| [expensesBackend/README.md](expensesBackend/README.md) | Backend project overview |
| [expensesBackend/API_EXAMPLES.md](expensesBackend/API_EXAMPLES.md) | API usage examples |
| [expensesBackend/QUICKSTART.md](expensesBackend/QUICKSTART.md) | Backend setup guide |

### Frontend Documentation
| Location | Purpose |
|----------|---------|
| [webapps/README.md](webapps/README.md) | Frontend project overview |
| [webapps/DOCUMENTATION.md](webapps/DOCUMENTATION.md) | Frontend detailed docs |
| [webapps/COMPONENT_GUIDE.md](webapps/COMPONENT_GUIDE.md) | Component library |
| [webapps/QUICKSTART.md](webapps/QUICKSTART.md) | Frontend setup guide |

---

## 🚀 Quick Start (Copy-Paste)

### Option 1: Automatic (Windows)
```powershell
cd d:\flutterRepo\expenseTracker
.\start.ps1
```

### Option 2: Manual
```powershell
# Terminal 1 - Backend
cd d:\flutterRepo\expenseTracker\expensesBackend
dotnet run

# Terminal 2 - Frontend
cd d:\flutterRepo\expenseTracker\webapps
npm run dev
```

**Then open:** http://localhost:3000

---

## 📋 Document Guide

### For Different User Types

#### I want to...

**Get the app running ASAP**
→ Go to [COMMANDS.md](COMMANDS.md)

**Understand how it's integrated**
→ Go to [ARCHITECTURE.md](ARCHITECTURE.md)

**Set up everything step-by-step**
→ Go to [INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md)

**Fix issues/troubleshoot**
→ Go to [INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md#troubleshooting)

**Understand API endpoints**
→ Go to [API_MAPPING.md](API_MAPPING.md) and [expensesBackend/API_EXAMPLES.md](expensesBackend/API_EXAMPLES.md)

**Develop frontend features**
→ Go to [webapps/DOCUMENTATION.md](webapps/DOCUMENTATION.md)

**Develop backend features**
→ Go to [expensesBackend/README.md](expensesBackend/README.md)

**See current status**
→ Go to [INTEGRATION_STATUS.md](INTEGRATION_STATUS.md)

---

## 🛠️ System Requirements

### Required Software
- **MongoDB** - Database
  - Download: https://www.mongodb.com/try/download/community
  - Or use MongoDB Atlas (cloud)

- **.NET 8 SDK** - Backend runtime
  - Download: https://dotnet.microsoft.com/download/dotnet/8.0
  - Verify: `dotnet --version`

- **Node.js 18+** - Frontend runtime
  - Download: https://nodejs.org/
  - Verify: `node --version`

### Recommended Tools
- **MongoDB Compass** - Database GUI
  - Download: https://www.mongodb.com/products/compass

- **Postman** - API testing
  - Download: https://www.postman.com/

- **VS Code** - Code editor
  - Download: https://code.visualstudio.com/

---

## 🔌 Service URLs

| Service | URL | Status |
|---------|-----|--------|
| Frontend | http://localhost:3000 | User interface |
| Backend | http://localhost:5196 | REST API |
| Swagger Docs | http://localhost:5196/swagger | API documentation |
| MongoDB | mongodb://localhost:27017 | Database |

---

## 📊 Project Statistics

```
Frontend (React)
├─ Pages: 6 (Auth, Dashboard, Expenses, Budget, Insights, Settings)
├─ Components: 8+ (Button, Card, Input, Modal, Select, Sidebar, TopBar, Loading)
├─ Services: 7+ (API, Auth, Expense, Dashboard, Budget, Forecast, Settings)
└─ Hooks: 4+ (useAuth, useApi, useTheme, useMediaQuery)

Backend (.NET)
├─ Controllers: 3 (Auth, Expenses, Dashboard)
├─ Services: 3+ (Auth, Expense, Dashboard)
├─ Entities: 5 (User, Expense, Budget, Category, RecurringExpense)
└─ DTOs: 4+ (ApiResponse, AuthDTOs, ExpenseDTOs, DashboardDTOs)

Database (MongoDB)
├─ Collections: 5 (users, expenses, budgets, categories, recurringExpenses)
└─ Database: ExpenseTrackerDB
```

---

## ✅ Integration Checklist

- [x] Frontend `.env` configured
- [x] Backend `appsettings.json` configured
- [x] CORS policy set for frontend
- [x] API endpoints aligned (capitalized routes)
- [x] JWT authentication enabled
- [x] MongoDB connection configured
- [x] Vite proxy configured
- [x] Startup scripts created (PS1 & BAT)
- [x] Documentation complete
- [ ] Budget controller implementation (TODO)
- [ ] Forecast service implementation (TODO)

---

## 🚨 Known Issues

### 1. Budget Controller Missing
**Status**: ⚠️ Not implemented
**Impact**: Budget features won't work
**Fix**: Create Budget controller and service

**Required endpoints:**
```
GET    /api/Budget
GET    /api/Budget/{id}
POST   /api/Budget
PUT    /api/Budget/{id}
DELETE /api/Budget/{id}
```

---

## 📞 Getting Help

### Troubleshooting Guide
1. Check [INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md#troubleshooting)
2. Review terminal logs
3. Use browser DevTools (F12)
4. Check MongoDB connection
5. Verify all prerequisites installed

### Common Issues

**MongoDB Not Running**
```powershell
mongod --dbpath "C:\data\db"
```

**Port Already in Use**
```powershell
Get-NetTCPConnection -LocalPort 5196
Get-Process -Id <PID> | Stop-Process -Force
```

**API Not Responding**
- Verify backend is running
- Check .env API URL
- Restart frontend dev server
- Check CORS configuration

**Database Connection Failed**
- Start MongoDB service
- Update connection string in appsettings.json
- Verify MongoDB is on port 27017

---

## 📝 File Organization

```
expenseTracker/
├── Documentation Root (you are here)
│   ├── README.md ..................... Project overview
│   ├── COMMANDS.md ................... CLI commands
│   ├── INTEGRATION_GUIDE.md ......... Detailed setup
│   ├── INTEGRATION_STATUS.md ........ Status & checklist
│   ├── ARCHITECTURE.md .............. System design
│   ├── API_MAPPING.md ............... Endpoint reference
│   ├── INDEX.md ..................... This file
│   ├── start.ps1 .................... PowerShell startup
│   ├── start.bat .................... Batch startup
│   └── QUICKSTART_CHECKLIST.md ...... Quick reference
│
├── expensesBackend/
│   ├── README.md .................... Backend overview
│   ├── API_EXAMPLES.md .............. Usage examples
│   ├── QUICKSTART.md ................ Setup guide
│   ├── Program.cs ................... Startup config
│   ├── appsettings.json ............. Database config
│   ├── Controllers/ ................. API endpoints
│   ├── Services/ .................... Business logic
│   ├── Domain/ ....................... Data models
│   └── Infrastructure/ .............. Database layer
│
└── webapps/
    ├── README.md .................... Frontend overview
    ├── DOCUMENTATION.md ............. Detailed docs
    ├── COMPONENT_GUIDE.md ........... Component library
    ├── QUICKSTART.md ................ Setup guide
    ├── .env ......................... API configuration
    ├── vite.config.ts ............... Build config
    ├── tsconfig.json ................ TypeScript config
    ├── package.json ................. Dependencies
    ├── src/
    │   ├── pages/ ................... Page components
    │   ├── components/ .............. UI components
    │   ├── services/ ................ API services
    │   ├── hooks/ ................... React hooks
    │   ├── types/ ................... TypeScript types
    │   ├── styles/ .................. CSS files
    │   └── layouts/ ................. Layout components
    └── dist/ ........................ Build output
```

---

## 🎓 Learning Path

### Beginner (Getting Started)
1. Read [README.md](README.md)
2. Run [COMMANDS.md](COMMANDS.md)
3. Complete [QUICKSTART_CHECKLIST.md](QUICKSTART_CHECKLIST.md)
4. Create your first account

### Intermediate (Development)
1. Read [ARCHITECTURE.md](ARCHITECTURE.md)
2. Study [API_MAPPING.md](API_MAPPING.md)
3. Review [webapps/DOCUMENTATION.md](webapps/DOCUMENTATION.md)
4. Review [expensesBackend/README.md](expensesBackend/README.md)
5. Start making features

### Advanced (Customization)
1. Study codebase architecture
2. Read detailed backend docs
3. Read detailed frontend docs
4. Implement custom features
5. Deploy to production

---

## 🔐 Security Notes

Before production deployment:
- [ ] Change JWT secret in `appsettings.json`
- [ ] Use environment variables for sensitive data
- [ ] Enable HTTPS
- [ ] Update CORS policy for production domain
- [ ] Use strong MongoDB credentials
- [ ] Enable MongoDB authentication
- [ ] Set up database backups
- [ ] Review and restrict API permissions

---

## 📈 Next Steps

1. **Get it running**
   - Follow [COMMANDS.md](COMMANDS.md)

2. **Understand the system**
   - Read [ARCHITECTURE.md](ARCHITECTURE.md)

3. **Start developing**
   - Frontend: See [webapps/DOCUMENTATION.md](webapps/DOCUMENTATION.md)
   - Backend: See [expensesBackend/README.md](expensesBackend/README.md)

4. **Test thoroughly**
   - Use Swagger UI at `/swagger`
   - Use Postman for API testing
   - Test all user flows

5. **Deploy**
   - Follow deployment sections in guides
   - Set up CI/CD if needed

---

## 📞 Support Resources

- **API Docs**: http://localhost:5196/swagger
- **Frontend Docs**: [webapps/DOCUMENTATION.md](webapps/DOCUMENTATION.md)
- **Backend Docs**: [expensesBackend/README.md](expensesBackend/README.md)
- **Troubleshooting**: [INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md#troubleshooting)

---

**Welcome to Expense Tracker!** 🎉

Start with the quick start commands and refer back to these docs as needed.

Last Updated: February 2, 2026
