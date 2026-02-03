# ExpenseTracker - Project Documentation

## 📋 Overview

A mobile-first expense tracking web application built with React, TypeScript, and modern web technologies. The application features a sleek fintech design with comprehensive expense management, budget tracking, and financial forecasting capabilities.

## 🏗️ Architecture & Component Hierarchy

### Project Structure

```
src/
├── components/          # Reusable UI components
│   ├── Button/         # Button component with variants
│   ├── Card/           # Card layout components
│   ├── Input/          # Input and Textarea components
│   ├── Select/         # Select dropdown component
│   ├── Modal/          # Modal dialog component
│   ├── Loading/        # Loading, EmptyState, ErrorState components
│   ├── Sidebar/        # Navigation sidebar (desktop) / bottom nav (mobile)
│   └── TopBar/         # Top navigation bar with search and user menu
│
├── layouts/            # Layout components
│   └── AppLayout/      # Main app layout with sidebar and topbar
│
├── pages/              # Page-level components
│   ├── Auth/           # Authentication pages (Login, Signup)
│   ├── Dashboard/      # Dashboard with summary and charts
│   ├── Expenses/       # Expense list and add expense pages
│   ├── Budget/         # Budget planner page
│   ├── Insights/       # Cash forecast and insights page
│   └── Settings/       # Settings and import page
│
├── hooks/              # Custom React hooks
│   ├── useAuth.tsx     # Authentication context and hook
│   ├── useTheme.ts     # Theme management hook
│   ├── useApi.ts       # API data fetching hooks
│   └── useMediaQuery.ts # Responsive breakpoint hooks
│
├── services/           # API service layer
│   ├── api.service.ts  # Base API service with HTTP methods
│   ├── auth.api.ts     # Authentication endpoints
│   ├── dashboard.api.ts # Dashboard endpoints
│   ├── expense.api.ts  # Expense CRUD endpoints
│   ├── budget.api.ts   # Budget endpoints
│   ├── forecast.api.ts # Forecast and simulation endpoints
│   └── settings.api.ts # Settings and import endpoints
│
├── types/              # TypeScript type definitions
│   └── index.ts        # All application types
│
├── utils/              # Utility functions
│   └── helpers.ts      # Helper functions (formatting, validation, etc.)
│
├── styles/             # Global styles
│   └── global.css      # CSS variables, themes, and global styles
│
├── App.tsx             # Main app component with routing
└── main.tsx            # Application entry point
```

## 🎨 Design System

### Color Palette

**Light Mode:**
- Primary: `#6366f1` (Indigo)
- Success: `#10b981` (Green)
- Warning: `#f59e0b` (Amber)
- Danger: `#ef4444` (Red)
- Background: `#f9fafb`
- Surface: `#ffffff`
- Text Primary: `#111827`
- Text Secondary: `#6b7280`

**Dark Mode:**
- Primary: `#818cf8` (Light Indigo)
- Background: `#0f172a`
- Surface: `#1e293b`
- Text Primary: `#f1f5f9`

### Typography
- Font Family: System font stack (San Francisco, Segoe UI, Roboto, etc.)
- Font Sizes: 12px - 36px (xs to 4xl)
- Font Weights: 400, 500, 600, 700

### Spacing
- Uses 4px base unit
- Scale: 4px, 8px, 16px, 24px, 32px, 48px, 64px

### Border Radius
- Small: 6px
- Medium: 8px
- Large: 12px
- XL: 16px
- Full: 9999px (pill shape)

### Shadows
- Soft shadows for cards
- Elevation-based shadow system

## 🧩 Component API

### Button Component

```tsx
<Button
  variant="primary" | "secondary" | "success" | "warning" | "danger" | "ghost"
  size="small" | "default" | "large"
  fullWidth={boolean}
  loading={boolean}
  onClick={function}
>
  Button Text
</Button>
```

### Card Component

```tsx
<Card variant="default" | "outline" | "elevated" size="default" | "compact">
  <CardHeader>
    <CardTitle>Title</CardTitle>
    <CardSubtitle>Subtitle</CardSubtitle>
  </CardHeader>
  <CardContent>Content</CardContent>
  <CardFooter>Footer</CardFooter>
</Card>
```

### Input Component

```tsx
<Input
  label="Label"
  type="text"
  placeholder="Placeholder"
  error="Error message"
  hint="Hint text"
  required={boolean}
  icon={<Icon />}
  inputSize="small" | "default" | "large"
/>
```

### Select Component

```tsx
<Select
  label="Label"
  options={[
    { value: "1", label: "Option 1" },
    { value: "2", label: "Option 2" }
  ]}
  placeholder="Select..."
  error="Error message"
  required={boolean}
/>
```

### Modal Component

```tsx
<Modal
  isOpen={boolean}
  onClose={function}
  title="Modal Title"
  size="small" | "default" | "large" | "full"
  footer={<Footer />}
  closeOnOverlayClick={boolean}
  closeOnEscape={boolean}
>
  Modal Content
</Modal>
```

## 🔌 API Integration Pattern

### Using the useApi Hook

```tsx
import { useApi } from '@/hooks/useApi';
import { dashboardApi } from '@/services/dashboard.api';

const { data, isLoading, error, refetch } = useApi(
  () => dashboardApi.getSummary(),
  { immediate: true }
);
```

### Using the useMutation Hook

```tsx
import { useMutation } from '@/hooks/useApi';
import { expenseApi } from '@/services/expense.api';

const { mutate, isLoading, error } = useMutation(
  (expense) => expenseApi.createExpense(expense),
  {
    onSuccess: (data) => console.log('Success:', data),
    onError: (error) => console.error('Error:', error)
  }
);

// Usage
await mutate(newExpense);
```

## 🔐 Authentication Flow

1. User enters email/phone on login/signup page
2. System sends OTP to provided contact
3. User enters 6-digit OTP
4. System verifies OTP and issues JWT token
5. Token stored in localStorage
6. Protected routes check authentication status
7. Auto-redirect based on auth state

## 📱 Responsive Design

### Breakpoints
- Mobile: < 768px
- Tablet: 768px - 1023px
- Desktop: ≥ 1024px
- Large Desktop: ≥ 1280px

### Mobile-First Approach
- All components designed for mobile first
- Progressive enhancement for larger screens
- Sidebar becomes bottom navigation on mobile
- Cards stack vertically on mobile
- Touch-friendly targets (minimum 44px)

## 🎯 Key Features Implementation

### Dashboard (Screen 1)
- Summary cards with color-coded status indicators
- Pie chart for top 10 categories (Recharts)
- Recent transactions with filters
- Responsive grid layout

### Add Expense (Screen 2)
- Large numeric input for amount
- Date picker
- Category and payment method selectors
- Recurring expense toggle with frequency options
- Sticky bottom save button on mobile

### Expense List (Screen 3)
- Filter chips for date range and category
- Vertical cards on mobile, table on desktop
- Search functionality
- Floating action button for quick add

### Budget Planner (Screen 4)
- Monthly overview card
- Category-wise budget cards with progress bars
- Color-coded warnings (green/amber/red)
- Inline editing capability

### Cash Forecast (Screen 6)
- Cash runway display (days remaining)
- Line chart for projected balance
- Breakdown cards for income/expenses
- Reassuring explanatory text

### Simulator (Screen 7)
- Input form for hypothetical purchase
- Impact analysis cards
- Color-coded recommendation banner
- Conversational tone

### Settings (Screen 8)
- Theme toggle
- Currency selection
- Category management
- CSV import with preview
- Account management

## 🚀 Getting Started

### Installation

```bash
# Install dependencies
npm install

# Start development server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview
```

### Environment Variables

Create a `.env` file:

```
VITE_API_BASE_URL=http://localhost:8000/api
```

### Development Workflow

1. Start the dev server: `npm run dev`
2. Open http://localhost:3000
3. Make changes - hot reload enabled
4. All API calls will go to the configured backend

## 🔧 State Management Approach

### Authentication State
- Managed via React Context (`AuthProvider`)
- Accessible through `useAuth()` hook
- Persisted in localStorage

### Theme State
- Managed via custom hook (`useTheme`)
- Stored in localStorage
- Applied via data-theme attribute

### API State
- Managed via custom hooks (`useApi`, `useMutation`)
- Loading, error, and data states
- Automatic refetch capability

### Local Component State
- React useState for form inputs
- React useState for UI toggles

## 📦 Backend API Requirements

The frontend expects these API endpoints:

### Authentication
- `POST /api/auth/request-otp` - Send OTP
- `POST /api/auth/login` - Login with OTP
- `POST /api/auth/signup` - User registration
- `POST /api/auth/verify-otp` - Verify OTP
- `GET /api/auth/me` - Get current user
- `POST /api/auth/logout` - Logout

### Dashboard
- `GET /api/dashboard` - Get dashboard summary

### Expenses
- `GET /api/expenses` - List expenses (with filters)
- `GET /api/expenses/:id` - Get single expense
- `POST /api/expenses` - Create expense
- `PUT /api/expenses/:id` - Update expense
- `DELETE /api/expenses/:id` - Delete expense
- `GET /api/expenses/recurring` - Get recurring expenses

### Budgets
- `GET /api/budgets` - List budgets
- `GET /api/budgets/:id` - Get budget
- `POST /api/budgets` - Create/update budget
- `PUT /api/budgets/:id` - Update budget
- `DELETE /api/budgets/:id` - Delete budget

### Forecast
- `GET /api/forecast` - Get cash forecast
- `POST /api/forecast/simulate` - Simulate purchase impact

### Settings
- `GET /api/settings` - Get settings
- `PUT /api/settings` - Update settings
- `GET /api/settings/categories` - Get categories
- `POST /api/settings/categories` - Create category
- `POST /api/import/preview` - Preview CSV import
- `POST /api/import/confirm` - Confirm import

## 🎨 Customization Guide

### Adding New Colors

Edit `src/styles/global.css`:

```css
:root {
  --color-custom: #yourcolor;
}

[data-theme="dark"] {
  --color-custom: #yourcolor;
}
```

### Adding New Components

1. Create component folder in `src/components/`
2. Create `.tsx` and `.module.css` files
3. Export from index file (optional)
4. Use in pages

### Adding New Pages

1. Create page file in `src/pages/`
2. Add route in `src/App.tsx`
3. Add navigation item in `Sidebar.tsx`

## 🐛 Common Issues & Solutions

### Port already in use
```bash
# Change port in vite.config.ts
server: { port: 3001 }
```

### API calls failing
- Check VITE_API_BASE_URL in .env
- Ensure backend is running
- Check browser console for CORS errors

### Theme not persisting
- Check localStorage permissions
- Clear browser cache
- Check data-theme attribute on <html>

## 📝 Best Practices

1. **Always use CSS variables** for colors and spacing
2. **Mobile-first** - design for mobile, enhance for desktop
3. **API-driven** - no hardcoded business logic
4. **Accessible** - use semantic HTML and ARIA labels
5. **Type-safe** - leverage TypeScript types
6. **Error handling** - always handle loading and error states
7. **Performance** - lazy load routes, optimize images
8. **Testing** - write tests for critical paths

## 🔄 Future Enhancements

- [ ] Offline support with service workers
- [ ] Export to PDF
- [ ] Multi-currency support
- [ ] Shared budgets (family accounts)
- [ ] Bank integration
- [ ] Receipt scanning with OCR
- [ ] Advanced analytics
- [ ] Budget templates
- [ ] Expense tags
- [ ] Voice input for quick adds

## 📄 License

Proprietary - All rights reserved

---

**Built with ❤️ using React + TypeScript + Vite**
