# Component Guide - Quick Reference

## 📚 Table of Contents

1. [Layout Components](#layout-components)
2. [UI Components](#ui-components)
3. [Page Components](#page-components)
4. [Custom Hooks](#custom-hooks)
5. [Usage Examples](#usage-examples)

---

## Layout Components

### AppLayout

Main application layout wrapper with sidebar and topbar.

```tsx
import { AppLayout } from '@/layouts/AppLayout/AppLayout';

<AppLayout>
  <YourPageContent />
</AppLayout>
```

**Features:**
- Responsive sidebar (desktop) / bottom nav (mobile)
- Fixed top bar with search and user menu
- Automatic layout adjustments

### Sidebar

Navigation sidebar component.

**Features:**
- Collapsible on desktop
- Transforms to bottom navigation on mobile
- Active state highlighting
- Icon + label navigation items

### TopBar

Top navigation bar component.

**Features:**
- App branding
- Global search bar (desktop only)
- Theme toggle
- Notification bell
- User avatar menu
- Responsive design

---

## UI Components

### Button

Versatile button component with multiple variants.

```tsx
import { Button } from '@/components/Button/Button';

// Primary button
<Button variant="primary" onClick={handleClick}>
  Save
</Button>

// Loading state
<Button loading={isLoading}>
  Submitting...
</Button>

// Full width
<Button fullWidth>
  Continue
</Button>

// With icon
<Button variant="success">
  ✓ Confirm
</Button>
```

**Props:**
- `variant`: 'primary' | 'secondary' | 'success' | 'warning' | 'danger' | 'ghost'
- `size`: 'small' | 'default' | 'large'
- `fullWidth`: boolean
- `loading`: boolean
- `disabled`: boolean
- `onClick`: function
- All standard button props

**Variants:**
- **primary** - Main action button (indigo)
- **secondary** - Outline button
- **success** - Positive action (green)
- **warning** - Caution action (amber)
- **danger** - Destructive action (red)
- **ghost** - Transparent button

---

### Card

Container component for content sections.

```tsx
import { Card, CardHeader, CardTitle, CardSubtitle, CardContent, CardFooter } from '@/components/Card/Card';

<Card variant="elevated">
  <CardHeader>
    <CardTitle>Title</CardTitle>
    <CardSubtitle>Subtitle text</CardSubtitle>
  </CardHeader>
  <CardContent>
    Main content here
  </CardContent>
  <CardFooter>
    Footer content
  </CardFooter>
</Card>
```

**Props:**
- `variant`: 'default' | 'outline' | 'elevated'
- `size`: 'default' | 'compact'
- `clickable`: boolean
- `onClick`: function

**Variants:**
- **default** - Standard card with shadow
- **outline** - Border instead of shadow
- **elevated** - Larger shadow for prominence

---

### Input

Text input component with label and validation.

```tsx
import { Input } from '@/components/Input/Input';

// Basic input
<Input
  label="Email"
  type="email"
  placeholder="Enter your email"
  value={email}
  onChange={(e) => setEmail(e.target.value)}
  required
/>

// With error
<Input
  label="Amount"
  type="number"
  error="Please enter a valid amount"
  value={amount}
  onChange={(e) => setAmount(e.target.value)}
/>

// With icon
<Input
  label="Search"
  icon={<span>🔍</span>}
  placeholder="Search..."
/>

// Different sizes
<Input inputSize="large" />
```

**Props:**
- `label`: string
- `type`: standard HTML input types
- `placeholder`: string
- `value`: string
- `onChange`: function
- `error`: string (displays error message)
- `hint`: string (helper text)
- `required`: boolean
- `icon`: ReactNode
- `inputSize`: 'small' | 'default' | 'large'
- All standard input props

---

### Textarea

Multi-line text input component.

```tsx
import { Textarea } from '@/components/Input/Input';

<Textarea
  label="Notes"
  placeholder="Add notes..."
  rows={4}
  value={notes}
  onChange={(e) => setNotes(e.target.value)}
/>
```

**Props:**
- `label`: string
- `placeholder`: string
- `rows`: number
- `error`: string
- `hint`: string
- `required`: boolean
- All standard textarea props

---

### Select

Dropdown select component.

```tsx
import { Select } from '@/components/Select/Select';

<Select
  label="Category"
  options={[
    { value: 'food', label: 'Food & Dining' },
    { value: 'transport', label: 'Transportation' },
    { value: 'shopping', label: 'Shopping' }
  ]}
  value={category}
  onChange={(e) => setCategory(e.target.value)}
  placeholder="Choose category"
  required
/>
```

**Props:**
- `label`: string
- `options`: Array<{ value: string, label: string }>
- `value`: string
- `onChange`: function
- `placeholder`: string
- `error`: string
- `hint`: string
- `required`: boolean
- All standard select props

---

### Modal

Dialog/overlay component.

```tsx
import { Modal } from '@/components/Modal/Modal';

<Modal
  isOpen={isOpen}
  onClose={() => setIsOpen(false)}
  title="Confirm Action"
  size="default"
  footer={
    <>
      <Button variant="ghost" onClick={() => setIsOpen(false)}>Cancel</Button>
      <Button onClick={handleConfirm}>Confirm</Button>
    </>
  }
>
  <p>Are you sure you want to continue?</p>
</Modal>
```

**Props:**
- `isOpen`: boolean (required)
- `onClose`: function (required)
- `title`: string
- `size`: 'small' | 'default' | 'large' | 'full'
- `footer`: ReactNode
- `closeOnOverlayClick`: boolean (default: true)
- `closeOnEscape`: boolean (default: true)

**Features:**
- Auto-focuses and traps focus
- Closes on ESC key
- Closes on overlay click (configurable)
- Portal-based rendering
- Smooth animations
- Scrollable content

---

### Loading

Loading spinner component.

```tsx
import { Loading } from '@/components/Loading/Loading';

// Basic spinner
<Loading />

// With text
<Loading text="Loading data..." />

// Different sizes
<Loading size="small" />
<Loading size="large" />
```

**Props:**
- `size`: 'small' | 'default' | 'large'
- `text`: string (optional loading message)

---

### EmptyState

Empty state placeholder component.

```tsx
import { EmptyState } from '@/components/Loading/Loading';

<EmptyState
  icon="📭"
  title="No expenses found"
  description="Start tracking by adding your first expense"
  action={
    <Button onClick={() => navigate('/expenses/add')}>
      Add Expense
    </Button>
  }
/>
```

**Props:**
- `icon`: string (emoji or icon)
- `title`: string (required)
- `description`: string
- `action`: ReactNode (optional CTA button)

---

### ErrorState

Error display component.

```tsx
import { ErrorState } from '@/components/Loading/Loading';

<ErrorState
  title="Failed to load"
  description="Unable to fetch data. Please try again."
  onRetry={() => refetch()}
/>
```

**Props:**
- `title`: string (default: "Something went wrong")
- `description`: string (required)
- `onRetry`: function (shows retry button)

---

## Custom Hooks

### useAuth

Authentication hook with context.

```tsx
import { useAuth } from '@/hooks/useAuth';

function MyComponent() {
  const { user, isAuthenticated, isLoading, login, logout } = useAuth();

  if (isLoading) return <Loading />;
  if (!isAuthenticated) return <LoginPrompt />;

  return <div>Welcome, {user.name}!</div>;
}
```

**Returns:**
- `user`: User | null
- `isAuthenticated`: boolean
- `isLoading`: boolean
- `login(email, otp)`: async function
- `signup(name, emailOrPhone)`: async function
- `logout()`: async function

---

### useTheme

Theme management hook.

```tsx
import { useTheme } from '@/hooks/useTheme';

function ThemeToggle() {
  const { theme, toggleTheme } = useTheme();

  return (
    <button onClick={toggleTheme}>
      {theme === 'light' ? '🌙 Dark' : '☀️ Light'}
    </button>
  );
}
```

**Returns:**
- `theme`: 'light' | 'dark'
- `setTheme(theme)`: function
- `toggleTheme()`: function

---

### useApi

Data fetching hook.

```tsx
import { useApi } from '@/hooks/useApi';
import { dashboardApi } from '@/services/dashboard.api';

function Dashboard() {
  const { data, isLoading, error, refetch } = useApi(
    () => dashboardApi.getSummary(),
    { 
      immediate: true,
      onSuccess: (data) => console.log('Loaded:', data)
    }
  );

  if (isLoading) return <Loading />;
  if (error) return <ErrorState description={error} onRetry={refetch} />;

  return <div>{/* Render data */}</div>;
}
```

**Parameters:**
- `apiFunc`: () => Promise<ApiResponse<T>>
- `options`: object
  - `immediate`: boolean (fetch on mount)
  - `initialData`: T
  - `onSuccess`: (data: T) => void
  - `onError`: (error: Error) => void

**Returns:**
- `data`: T | undefined
- `isLoading`: boolean
- `error`: string | null
- `refetch()`: function
- `execute()`: function

---

### useMutation

Mutation hook for POST/PUT/DELETE operations.

```tsx
import { useMutation } from '@/hooks/useApi';
import { expenseApi } from '@/services/expense.api';

function AddExpense() {
  const { mutate, isLoading, error } = useMutation(
    (expense) => expenseApi.createExpense(expense),
    {
      onSuccess: () => {
        alert('Expense added!');
        navigate('/expenses');
      }
    }
  );

  const handleSubmit = async (e) => {
    e.preventDefault();
    await mutate(formData);
  };

  return <form onSubmit={handleSubmit}>...</form>;
}
```

**Parameters:**
- `apiFunc`: (variables: TVariables) => Promise<ApiResponse<T>>
- `options`: object
  - `onSuccess`: (data: T) => void
  - `onError`: (error: Error) => void

**Returns:**
- `mutate(variables)`: async function
- `data`: T | undefined
- `isLoading`: boolean
- `error`: string | null
- `reset()`: function

---

### useMediaQuery

Responsive breakpoint hook.

```tsx
import { useMediaQuery, useBreakpoint } from '@/hooks/useMediaQuery';

// Check specific query
const isMobile = useMediaQuery('(max-width: 767px)');

// Get device type
const { isMobile, isTablet, isDesktop, deviceType } = useBreakpoint();
```

**useMediaQuery Returns:** boolean

**useBreakpoint Returns:**
- `isMobile`: boolean
- `isTablet`: boolean
- `isDesktop`: boolean
- `isLargeDesktop`: boolean
- `deviceType`: 'mobile' | 'tablet' | 'desktop'

---

## Usage Examples

### Complete Form Example

```tsx
import { useState } from 'react';
import { Card } from '@/components/Card/Card';
import { Input } from '@/components/Input/Input';
import { Select } from '@/components/Select/Select';
import { Button } from '@/components/Button/Button';
import { useMutation } from '@/hooks/useApi';

function ExpenseForm() {
  const [amount, setAmount] = useState('');
  const [category, setCategory] = useState('');
  
  const { mutate, isLoading, error } = useMutation(
    (data) => expenseApi.createExpense(data)
  );

  const handleSubmit = async (e) => {
    e.preventDefault();
    await mutate({ amount, category });
  };

  return (
    <Card>
      <form onSubmit={handleSubmit}>
        <Input
          label="Amount"
          type="number"
          value={amount}
          onChange={(e) => setAmount(e.target.value)}
          required
        />
        
        <Select
          label="Category"
          options={[
            { value: 'food', label: 'Food' },
            { value: 'transport', label: 'Transport' }
          ]}
          value={category}
          onChange={(e) => setCategory(e.target.value)}
          required
        />
        
        {error && <p style={{ color: 'red' }}>{error}</p>}
        
        <Button type="submit" loading={isLoading} fullWidth>
          Save Expense
        </Button>
      </form>
    </Card>
  );
}
```

### Data Fetching with Error Handling

```tsx
import { useApi } from '@/hooks/useApi';
import { Loading, ErrorState, EmptyState } from '@/components/Loading/Loading';

function ExpenseList() {
  const { data, isLoading, error, refetch } = useApi(
    () => expenseApi.getExpenses()
  );

  if (isLoading) return <Loading text="Loading expenses..." />;
  if (error) return <ErrorState description={error} onRetry={refetch} />;
  if (!data?.items.length) return <EmptyState title="No expenses" />;

  return (
    <div>
      {data.items.map(expense => (
        <ExpenseCard key={expense.id} expense={expense} />
      ))}
    </div>
  );
}
```

### Modal with Confirmation

```tsx
import { useState } from 'react';
import { Modal } from '@/components/Modal/Modal';
import { Button } from '@/components/Button/Button';

function DeleteButton({ expenseId }) {
  const [isOpen, setIsOpen] = useState(false);

  const handleDelete = async () => {
    await expenseApi.deleteExpense(expenseId);
    setIsOpen(false);
  };

  return (
    <>
      <Button variant="danger" onClick={() => setIsOpen(true)}>
        Delete
      </Button>

      <Modal
        isOpen={isOpen}
        onClose={() => setIsOpen(false)}
        title="Confirm Deletion"
        footer={
          <>
            <Button variant="ghost" onClick={() => setIsOpen(false)}>
              Cancel
            </Button>
            <Button variant="danger" onClick={handleDelete}>
              Delete
            </Button>
          </>
        }
      >
        <p>Are you sure you want to delete this expense?</p>
      </Modal>
    </>
  );
}
```

### Responsive Layout

```tsx
import { useBreakpoint } from '@/hooks/useMediaQuery';
import { Card } from '@/components/Card/Card';

function ResponsiveGrid() {
  const { isMobile } = useBreakpoint();

  return (
    <div style={{
      display: 'grid',
      gridTemplateColumns: isMobile ? '1fr' : 'repeat(3, 1fr)',
      gap: '1rem'
    }}>
      <Card>Card 1</Card>
      <Card>Card 2</Card>
      <Card>Card 3</Card>
    </div>
  );
}
```

---

## CSS Variables Quick Reference

### Colors
```css
var(--color-primary)
var(--color-success)
var(--color-warning)
var(--color-danger)
var(--color-background)
var(--color-surface)
var(--color-text-primary)
var(--color-text-secondary)
```

### Spacing
```css
var(--spacing-xs)    /* 4px */
var(--spacing-sm)    /* 8px */
var(--spacing-md)    /* 16px */
var(--spacing-lg)    /* 24px */
var(--spacing-xl)    /* 32px */
var(--spacing-2xl)   /* 48px */
```

### Shadows
```css
var(--shadow-sm)
var(--shadow-md)
var(--shadow-lg)
var(--shadow-xl)
```

### Border Radius
```css
var(--radius-sm)     /* 6px */
var(--radius-md)     /* 8px */
var(--radius-lg)     /* 12px */
var(--radius-xl)     /* 16px */
var(--radius-full)   /* 9999px */
```

### Transitions
```css
var(--transition-fast)   /* 150ms */
var(--transition-base)   /* 200ms */
var(--transition-slow)   /* 250ms */
```

---

**For more detailed documentation, see DOCUMENTATION.md**
