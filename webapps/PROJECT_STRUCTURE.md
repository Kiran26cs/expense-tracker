# 🗂️ Project Structure Visualization

## File Tree

```
d:\flutterRepo\webapps\
│
├── 📄 package.json                    # Project dependencies and scripts
├── 📄 tsconfig.json                   # TypeScript configuration
├── 📄 vite.config.ts                  # Vite build configuration
├── 📄 index.html                      # HTML entry point
├── 📄 .env.example                    # Environment variables template
├── 📄 .gitignore                      # Git ignore rules
│
├── 📚 README.md                       # Project overview
├── 📚 QUICKSTART.md                   # Quick start guide (START HERE!)
├── 📚 PROJECT_OVERVIEW.md             # High-level summary
├── 📚 DOCUMENTATION.md                # Complete technical documentation
├── 📚 COMPONENT_GUIDE.md              # Component API reference
├── 📚 BACKEND_INTEGRATION.md          # API integration checklist
│
└── 📁 src/
    │
    ├── 📄 main.tsx                    # Application entry point
    ├── 📄 App.tsx                     # Main app with routing
    ├── 📄 vite-env.d.ts              # Vite type definitions
    │
    ├── 📁 styles/
    │   └── 📄 global.css              # Global styles, CSS variables, themes
    │
    ├── 📁 types/
    │   └── 📄 index.ts                # TypeScript type definitions
    │
    ├── 📁 utils/
    │   └── 📄 helpers.ts              # Utility functions (format, validate, etc.)
    │
    ├── 📁 hooks/
    │   ├── 📄 useAuth.tsx             # Authentication context & hook
    │   ├── 📄 useTheme.ts             # Theme management hook
    │   ├── 📄 useApi.ts               # Data fetching hooks
    │   └── 📄 useMediaQuery.ts        # Responsive breakpoint hooks
    │
    ├── 📁 services/
    │   ├── 📄 api.service.ts          # Base API service
    │   ├── 📄 auth.api.ts             # Authentication endpoints
    │   ├── 📄 dashboard.api.ts        # Dashboard endpoints
    │   ├── 📄 expense.api.ts          # Expense CRUD endpoints
    │   ├── 📄 budget.api.ts           # Budget endpoints
    │   ├── 📄 forecast.api.ts         # Forecast & simulation endpoints
    │   └── 📄 settings.api.ts         # Settings & import endpoints
    │
    ├── 📁 components/
    │   │
    │   ├── 📁 Button/
    │   │   ├── 📄 Button.tsx          # Button component
    │   │   └── 📄 Button.module.css   # Button styles
    │   │
    │   ├── 📁 Card/
    │   │   ├── 📄 Card.tsx            # Card component system
    │   │   └── 📄 Card.module.css     # Card styles
    │   │
    │   ├── 📁 Input/
    │   │   ├── 📄 Input.tsx           # Input & Textarea components
    │   │   └── 📄 Input.module.css    # Input styles
    │   │
    │   ├── 📁 Select/
    │   │   ├── 📄 Select.tsx          # Select dropdown component
    │   │   └── 📄 Select.module.css   # Select styles
    │   │
    │   ├── 📁 Modal/
    │   │   ├── 📄 Modal.tsx           # Modal dialog component
    │   │   └── 📄 Modal.module.css    # Modal styles
    │   │
    │   ├── 📁 Loading/
    │   │   ├── 📄 Loading.tsx         # Loading, EmptyState, ErrorState
    │   │   └── 📄 Loading.module.css  # Loading styles
    │   │
    │   ├── 📁 Sidebar/
    │   │   ├── 📄 Sidebar.tsx         # Navigation sidebar
    │   │   └── 📄 Sidebar.module.css  # Sidebar styles
    │   │
    │   └── 📁 TopBar/
    │       ├── 📄 TopBar.tsx          # Top navigation bar
    │       └── 📄 TopBar.module.css   # TopBar styles
    │
    ├── 📁 layouts/
    │   └── 📁 AppLayout/
    │       ├── 📄 AppLayout.tsx       # Main app layout wrapper
    │       └── 📄 AppLayout.module.css # Layout styles
    │
    └── 📁 pages/
        │
        ├── 📁 Auth/
        │   ├── 📄 Auth.module.css      # Shared auth styles
        │   ├── 📄 LoginPage.tsx        # Login page (OTP-based)
        │   └── 📄 SignupPage.tsx       # Signup page (OTP-based)
        │
        ├── 📁 Dashboard/
        │   ├── 📄 Dashboard.module.css  # Dashboard styles
        │   └── 📄 DashboardPage.tsx    # Dashboard with summary & charts
        │
        ├── 📁 Expenses/
        │   ├── 📄 ExpenseListPage.tsx  # Expense list with filters
        │   └── 📄 AddExpensePage.tsx   # Add/edit expense form
        │
        ├── 📁 Budget/
        │   └── 📄 BudgetPage.tsx       # Budget planner
        │
        ├── 📁 Insights/
        │   └── 📄 InsightsPage.tsx     # Cash forecast & insights
        │
        └── 📁 Settings/
            └── 📄 SettingsPage.tsx     # Settings & import
```

## Component Hierarchy

```
App
├── BrowserRouter
│   └── AuthProvider
│       └── Routes
│           │
│           ├── Public Routes (Unauthenticated)
│           │   ├── /login → LoginPage
│           │   └── /signup → SignupPage
│           │
│           └── Protected Routes (Authenticated)
│               └── AppLayout
│                   ├── Sidebar (Navigation)
│                   ├── TopBar (Header)
│                   └── Main Content
│                       ├── / → DashboardPage
│                       ├── /expenses → ExpenseListPage
│                       ├── /expenses/add → AddExpensePage
│                       ├── /budget → BudgetPage
│                       ├── /insights → InsightsPage
│                       └── /settings → SettingsPage
```

## Data Flow Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                         User Interface                       │
│  (Pages & Components)                                        │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                     Custom Hooks Layer                       │
│  • useAuth() - Authentication state                          │
│  • useApi() - Data fetching with loading/error states       │
│  • useMutation() - Form submissions                          │
│  • useTheme() - Theme management                             │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                     API Services Layer                       │
│  • authApi - Authentication endpoints                        │
│  • expenseApi - Expense CRUD operations                      │
│  • budgetApi - Budget management                             │
│  • forecastApi - Cash forecast & simulation                  │
│  • settingsApi - Settings & import                           │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                    Base API Service                          │
│  • HTTP request wrapper (GET, POST, PUT, DELETE)             │
│  • Token management                                          │
│  • Error handling                                            │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                    Backend REST API                          │
│  (Your backend server)                                       │
└─────────────────────────────────────────────────────────────┘
```

## Page Component Structure Example

```
DashboardPage
├── Header Section
│   ├── Title
│   └── "Add Expense" Button
│
├── Summary Cards Grid
│   ├── Total Spent Card
│   ├── Remaining Budget Card
│   ├── Expected Savings Card
│   └── Cash Runway Card
│
├── Charts Section
│   ├── Category Breakdown (Pie Chart)
│   └── Spending Trend (Line Chart)
│
└── Recent Transactions
    ├── Filter Tabs
    ├── Transaction List
    └── "View All" Button
```

## State Management Flow

```
┌──────────────────────────────────────────────────────────┐
│                    Global State                           │
│                                                           │
│  AuthContext (via useAuth)                               │
│  ├── user: User | null                                   │
│  ├── isAuthenticated: boolean                            │
│  ├── isLoading: boolean                                  │
│  ├── login()                                             │
│  ├── signup()                                            │
│  └── logout()                                            │
│                                                           │
│  ThemeContext (via useTheme)                             │
│  ├── theme: 'light' | 'dark'                             │
│  ├── setTheme()                                          │
│  └── toggleTheme()                                       │
└──────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│                   Component State                         │
│                                                           │
│  Local state (useState)                                   │
│  ├── Form inputs                                          │
│  ├── Modal visibility                                     │
│  ├── Selected filters                                     │
│  └── UI toggles                                           │
│                                                           │
│  API state (useApi/useMutation)                           │
│  ├── data: API response                                   │
│  ├── isLoading: boolean                                   │
│  ├── error: string | null                                 │
│  └── refetch/mutate functions                             │
└──────────────────────────────────────────────────────────┘
```

## Theme System Architecture

```
:root (Light Mode)
├── Color Variables
│   ├── --color-primary: #6366f1
│   ├── --color-success: #10b981
│   ├── --color-warning: #f59e0b
│   ├── --color-danger: #ef4444
│   ├── --color-background: #f9fafb
│   ├── --color-surface: #ffffff
│   └── --color-text-primary: #111827
│
├── Spacing Variables
│   ├── --spacing-xs: 0.25rem
│   ├── --spacing-sm: 0.5rem
│   ├── --spacing-md: 1rem
│   └── --spacing-lg: 1.5rem
│
├── Typography Variables
│   ├── --font-size-sm: 0.875rem
│   ├── --font-size-base: 1rem
│   └── --font-size-lg: 1.125rem
│
└── Other Variables
    ├── Shadows
    ├── Border Radius
    └── Transitions

[data-theme="dark"] (Dark Mode)
└── Override color variables
    ├── --color-primary: #818cf8
    ├── --color-background: #0f172a
    ├── --color-surface: #1e293b
    └── --color-text-primary: #f1f5f9
```

## Routing Structure

```
/
├── /login (Public)
│   └── Login with email/phone + OTP
│
├── /signup (Public)
│   └── Signup with name + email/phone
│
└── Protected Routes (Requires Authentication)
    │
    ├── / (Dashboard)
    │   ├── Summary cards
    │   ├── Charts
    │   └── Recent transactions
    │
    ├── /expenses
    │   └── Expense list with filters
    │
    ├── /expenses/add
    │   └── Add expense form
    │
    ├── /budget
    │   └── Budget planner
    │
    ├── /insights
    │   ├── Cash runway
    │   ├── Forecast charts
    │   └── Purchase simulator
    │
    └── /settings
        ├── Theme toggle
        ├── Preferences
        └── CSV import
```

## Responsive Breakpoints

```
Mobile First Approach:

Base (Mobile)
  0px - 767px
  ↓
  • Sidebar → Bottom navigation
  • Single column layouts
  • Stacked cards
  • Touch-friendly targets (44px minimum)

Tablet
  768px - 1023px
  ↓
  • Sidebar appears
  • 2-column grids
  • Larger touch targets

Desktop
  1024px+
  ↓
  • Full sidebar
  • 3-4 column grids
  • Mouse-optimized interactions
  • Collapsible sidebar

Large Desktop
  1280px+
  ↓
  • Maximum container width
  • Optimized spacing
```

## File Naming Conventions

```
Components:
  • PascalCase for component files: Button.tsx
  • CSS Modules: ComponentName.module.css
  • Co-located with component

Pages:
  • PascalCase with "Page" suffix: DashboardPage.tsx
  • Organized in feature folders

Hooks:
  • camelCase with "use" prefix: useAuth.tsx
  • Custom hooks in hooks/ folder

Services:
  • camelCase with ".api" suffix: expense.api.ts
  • Organized by feature

Types:
  • PascalCase for interfaces: User, Expense
  • Centralized in types/index.ts

Utils:
  • camelCase for functions: formatCurrency
  • Grouped in utils/helpers.ts
```

## Import Path Aliases

```typescript
// Instead of:
import { Button } from '../../../components/Button/Button';

// Use:
import { Button } from '@/components/Button/Button';

Configured in:
  • tsconfig.json (TypeScript)
  • vite.config.ts (Vite bundler)
```

---

**This structure provides**:
- ✅ Clear separation of concerns
- ✅ Scalable architecture
- ✅ Easy to navigate
- ✅ Maintainable codebase
- ✅ Reusable components
- ✅ Type-safe development
