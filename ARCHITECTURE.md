# 🏗️ Application Architecture

## System Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     EXPENSE TRACKER APP                         │
└─────────────────────────────────────────────────────────────────┘

                          User Browser
                              │
                         localhost:3000
                              │
        ┌─────────────────────▼─────────────────────┐
        │   React + TypeScript Frontend (Vite)      │
        │   ├─ Pages (Auth, Dashboard, Expenses)   │
        │   ├─ Components (UI Components)          │
        │   ├─ Services (API calls)                │
        │   ├─ Hooks (useAuth, useApi, etc)       │
        │   └─ State Management                    │
        └─────────────────────┬─────────────────────┘
                              │
                HTTP REST API │ (JSON)
                              │
        ┌─────────────────────▼─────────────────────┐
        │  .NET 8 Backend API (localhost:5196)      │
        │  ├─ Controllers (Auth, Expenses, etc)    │
        │  ├─ Services (Business Logic)            │
        │  ├─ DTOs (Data Transfer Objects)         │
        │  ├─ Middleware (Auth, Exception Handler) │
        │  └─ Database Access Layer                │
        └─────────────────────┬─────────────────────┘
                              │
                MongoDB Query │ (BSON)
                              │
        ┌─────────────────────▼─────────────────────┐
        │   MongoDB Database                       │
        │   (localhost:27017)                      │
        │                                          │
        │   Collections:                           │
        │   ├─ users (authentication)              │
        │   ├─ expenses (expense records)          │
        │   ├─ budgets (budget settings)           │
        │   ├─ categories (expense categories)     │
        │   └─ recurringExpenses (recurring data)  │
        └──────────────────────────────────────────┘
```

## Communication Flow

### Authentication Flow
```
User Input (Email/Phone)
        ↓
Frontend: POST /Auth/send-otp
        ↓
Backend: AuthController.SendOtp()
        ↓
Backend: AuthService.SendOtpAsync()
        ↓
MongoDB: Store OTP
        ↓
Frontend: Display OTP verification dialog
        ↓
User Input (OTP)
        ↓
Frontend: POST /Auth/verify-otp
        ↓
Backend: AuthController.VerifyOtp()
        ↓
Backend: Validate OTP from MongoDB
        ↓
Frontend: POST /Auth/signup or /Auth/login
        ↓
Backend: Create/Verify user
        ↓
Backend: Generate JWT token
        ↓
Frontend: Store token in localStorage
        ↓
Frontend: Redirect to Dashboard
```

### Expense Management Flow
```
User Input (Expense details)
        ↓
Frontend: POST /Expenses
        ↓
Headers: Authorization: Bearer {JWT_TOKEN}
        ↓
Backend: ExpensesController.CreateExpense()
        ↓
Backend: Verify JWT token → Extract userId
        ↓
Backend: ExpenseService.CreateExpenseAsync()
        ↓
MongoDB: Save expense document
        ↓
Backend: Return created expense
        ↓
Frontend: Update UI with new expense
        ↓
Frontend: Show success notification
```

## Project Structure

```
expenseTracker/
│
├── expensesBackend/                    # .NET 8 API
│   ├── Controllers/
│   │   ├── AuthController.cs          # Login/Signup endpoints
│   │   ├── ExpensesController.cs      # CRUD operations
│   │   └── DashboardController.cs     # Summary & analytics
│   │
│   ├── Services/
│   │   ├── IAuthService.cs            # Auth interface
│   │   ├── AuthService.cs             # Authentication logic
│   │   ├── IExpenseService.cs         # Expense interface
│   │   ├── ExpenseService.cs          # Expense logic
│   │   └── [other services]
│   │
│   ├── Domain/
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── Expense.cs
│   │   │   ├── Budget.cs
│   │   │   ├── Category.cs
│   │   │   └── RecurringExpense.cs
│   │   │
│   │   └── DTOs/
│   │       ├── ApiResponse.cs         # Standard API response
│   │       ├── AuthDTOs.cs            # Auth request/response
│   │       ├── ExpenseDTOs.cs         # Expense data transfers
│   │       └── [other DTOs]
│   │
│   ├── Infrastructure/
│   │   ├── Data/
│   │   │   └── MongoDbContext.cs      # DB connection
│   │   │
│   │   └── Middleware/
│   │       └── GlobalExceptionHandler.cs
│   │
│   ├── Program.cs                      # Startup configuration
│   ├── appsettings.json               # Settings & connections
│   └── ExpensesBackend.API.csproj
│
├── webapps/                            # React + TypeScript Frontend
│   ├── src/
│   │   ├── pages/
│   │   │   ├── Auth/                  # Login/Signup pages
│   │   │   ├── Dashboard/             # Dashboard page
│   │   │   ├── Expenses/              # Expenses management
│   │   │   ├── Budget/                # Budget tracking
│   │   │   ├── Insights/              # Analytics
│   │   │   └── Settings/              # User settings
│   │   │
│   │   ├── components/                # Reusable UI components
│   │   │   ├── Button/
│   │   │   ├── Card/
│   │   │   ├── Input/
│   │   │   ├── Modal/
│   │   │   ├── Sidebar/
│   │   │   └── TopBar/
│   │   │
│   │   ├── services/                  # API client services
│   │   │   ├── api.service.ts         # HTTP client
│   │   │   ├── auth.api.ts            # Auth endpoints
│   │   │   ├── expense.api.ts         # Expense endpoints
│   │   │   ├── dashboard.api.ts       # Dashboard endpoints
│   │   │   └── [other API services]
│   │   │
│   │   ├── hooks/                     # Custom React hooks
│   │   │   ├── useAuth.tsx            # Auth context
│   │   │   ├── useApi.ts              # API calls
│   │   │   ├── useTheme.ts            # Theme management
│   │   │   └── useMediaQuery.ts       # Responsive design
│   │   │
│   │   ├── types/
│   │   │   └── index.ts               # TypeScript types
│   │   │
│   │   ├── styles/
│   │   │   └── global.css             # Global styles
│   │   │
│   │   ├── layouts/
│   │   │   └── AppLayout/             # Main layout
│   │   │
│   │   ├── App.tsx                    # Root component
│   │   └── main.tsx                   # Entry point
│   │
│   ├── .env                           # Environment variables
│   ├── vite.config.ts                # Vite configuration
│   ├── tsconfig.json                 # TypeScript config
│   └── package.json                  # Dependencies
│
└── Documentation/
    ├── README.md                       # Main overview
    ├── INTEGRATION_GUIDE.md           # Detailed setup
    ├── INTEGRATION_STATUS.md          # Status & checklist
    ├── QUICKSTART_CHECKLIST.md        # Quick reference
    ├── COMMANDS.md                    # CLI commands
    ├── API_MAPPING.md                 # API endpoint docs
    └── [other docs]
```

## Technology Stack

### Frontend
- **Framework**: React 18+ with TypeScript
- **Build Tool**: Vite
- **HTTP Client**: Fetch API
- **Styling**: CSS/TailwindCSS
- **State Management**: React Context API + Hooks
- **Authentication**: JWT (localStorage)

### Backend
- **Runtime**: .NET 8
- **Language**: C#
- **Database**: MongoDB
- **Authentication**: JWT Bearer Token
- **API Documentation**: Swagger/OpenAPI
- **Architecture**: MVC/Service Layer

### Database
- **Type**: NoSQL (MongoDB)
- **Collections**: users, expenses, budgets, categories, recurringExpenses
- **Connection**: MongoDB Atlas or Local

## API Endpoints Summary

### Authentication
```
POST   /api/Auth/send-otp       Send OTP for login/signup
POST   /api/Auth/verify-otp     Verify OTP code
POST   /api/Auth/signup         Register new user
POST   /api/Auth/login          Login existing user
GET    /api/Auth/me             Get current user
```

### Expenses
```
GET    /api/Expenses            Get all expenses
GET    /api/Expenses/{id}       Get single expense
POST   /api/Expenses            Create new expense
PUT    /api/Expenses/{id}       Update expense
DELETE /api/Expenses/{id}       Delete expense
GET    /api/Expenses/recurring  Get recurring expenses
```

### Dashboard
```
GET    /api/Dashboard           Get dashboard summary
GET    /api/Dashboard/summary   Get spending summary
GET    /api/Dashboard/trends    Get monthly trends
```

## Data Flow Example: Add Expense

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Add Expense Flow                             │
└─────────────────────────────────────────────────────────────────────┘

1. User Interaction
   └─> User fills expense form and clicks "Add"

2. Frontend Processing
   └─> Validate form data
   └─> Prepare request payload
   └─> Add JWT token to headers

3. HTTP Request
   └─> POST /api/Expenses
   └─> Headers: 
       - Content-Type: application/json
       - Authorization: Bearer {JWT_TOKEN}
   └─> Body: { amount, category, description, date, ... }

4. Backend Processing
   └─> ExpensesController receives request
   └─> Validate JWT token
   └─> Extract userId from claims
   └─> Call ExpenseService.CreateExpenseAsync()
   └─> Validate expense data
   └─> Create Expense entity

5. Database Operation
   └─> MongoDbContext.ExpensesCollection.InsertOneAsync()
   └─> MongoDB saves document with:
       - id (ObjectId)
       - userId
       - amount
       - category
       - description
       - date
       - createdAt
       - updatedAt

6. Response
   └─> Backend returns ApiResponse<Expense>
   └─> HTTP 200 OK with created expense

7. Frontend Update
   └─> Show success notification
   └─> Add expense to list
   └─> Update dashboard summary
   └─> Clear form
```

## Security Features

- ✅ JWT Token-based authentication
- ✅ Token stored in secure localStorage
- ✅ Authorization headers on all API requests
- ✅ CORS enabled only for trusted origins
- ✅ HTTPS support in production
- ✅ Password hashing (backend)
- ✅ OTP verification for signup/login

## Performance Considerations

- ✅ API proxy in Vite for development
- ✅ Pagination support for expenses
- ✅ Lazy loading for large datasets
- ✅ Caching of dashboard data
- ✅ Indexed MongoDB queries
- ✅ Async/await for non-blocking operations

---

**Architecture is production-ready!** All components are properly integrated and configured for communication.
