# Dashboard Testing Guide

## Overview
This guide verifies that the dashboard properly displays real data from the backend.

## Prerequisites
- Backend running on `http://localhost:5196`
- Frontend running on `http://localhost:3001` (or 3000)
- User authenticated and logged in
- At least 3-5 expenses added with different categories

## How to Add Test Data

### Method 1: Manual Addition via UI
1. Navigate to **Expenses** section
2. Click **"+ Add Expense"** button
3. Fill in the form:
   - **Amount**: 150
   - **Category**: food
   - **Date**: Today
   - **Description**: Lunch
   - **Payment Method**: Card
4. Click **Save**
5. Repeat 2-3 times with different categories:
   - food: 150, 100, 75
   - transport: 50, 30
   - shopping: 200
   - entertainment: 80

### Method 2: CSV Import
1. Go to **Expenses** section
2. Click **"Import from CSV"**
3. Download template
4. Add sample expenses
5. Upload file

### Method 3: API Testing (curl/Postman)
```bash
POST /api/expenses/create
Authorization: Bearer <YOUR_JWT_TOKEN>
Content-Type: application/json

{
  "amount": 150,
  "date": "2024-01-15",
  "category": "food",
  "paymentMethod": "card",
  "description": "Lunch at restaurant",
  "isRecurring": false
}
```

## Verification Steps

### Step 1: Check Dashboard Loads
- [ ] Navigate to Dashboard
- [ ] Page loads without errors
- [ ] No console errors (check browser DevTools)
- [ ] All sections visible

### Step 2: Verify Summary Cards
- [ ] **Total Spent**: Shows sum of all expenses
- [ ] **Total Income**: Shows user's monthly income (from signup)
- [ ] **Savings**: Equals Income - Spent
- [ ] **Save Rate**: Calculates correctly (Savings / Income * 100)

**Status Indicator** should show:
- Green ✓ if save rate > 20%
- Yellow ⚠ if 80-100% of income spent
- Red ⚡ if spending > income

Example: If income = 5000, spent = 3500
- Savings = 1500
- Save Rate = 30%
- Status = "✓ On Track" (green)

### Step 3: Verify Charts Display

#### Pie Chart (Spending by Category)
- [ ] Chart renders (not "No expense data" message)
- [ ] Shows all categories with expenses
- [ ] Each segment has different color
- [ ] Labels show category name and percentage
- [ ] Total of all percentages ≈ 100%
- [ ] Example: food 45%, transport 20%, shopping 25%, entertainment 10%

#### Bar Chart (Category Amounts)
- [ ] Chart renders with all categories
- [ ] Bars represent correct amounts
- [ ] Heights correspond to spending amounts
- [ ] Tallest bar = highest spending category
- [ ] X-axis labels readable (angled)
- [ ] Y-axis shows currency amounts

### Step 4: Verify Recent Transactions

- [ ] Shows up to 10 most recent transactions
- [ ] Displays in correct order (newest first)
- [ ] Each transaction shows:
  - [ ] Category icon (emoji)
  - [ ] Description (truncated if > 50 chars)
  - [ ] Category name
  - [ ] Relative date ("2 hours ago", "1 day ago")
  - [ ] Payment method
  - [ ] Amount formatted as currency

Example Row:
```
🍔  Lunch at restaurant    food • 2 hours ago • Card    $150.00
```

### Step 5: Verify Empty States
- [ ] If no expenses: Shows "No expense data" with proper styling
- [ ] If no transactions: Shows "No transactions yet" with button to add
- [ ] Buttons in empty states are clickable and navigate correctly

### Step 6: Responsive Design
Test on different screen sizes:
- [ ] Desktop (1920x1080): All charts side-by-side
- [ ] Laptop (1366x768): Charts start stacking
- [ ] Tablet (768px): Single column layout
- [ ] Mobile (375px): All sections stacked vertically

### Step 7: Functionality
- [ ] Click "View All Transactions" → Routes to /expenses
- [ ] Click "+ Add Expense" → Routes to /expenses/add
- [ ] Click "Add First Expense" (empty state) → Routes to /expenses/add
- [ ] Dashboard updates after adding new expense

## Data Validation Checklist

### Calculations
- [ ] Total Spent = Sum of all expense amounts
- [ ] Category percentages = (Category Amount / Total Spent) × 100
- [ ] Savings = Total Income - Total Spent
- [ ] Save Rate = (Savings / Total Income) × 100

### Formatting
- [ ] Amounts use correct currency symbol ($ for USD)
- [ ] Amounts rounded to 2 decimal places
- [ ] Dates shown as relative ("2 hours ago")
- [ ] Percentage shown with 1 decimal place (45.5%)

## Example Scenario

**Setup:**
- User income: $5000
- Expenses added:
  - 🍔 Food: $300 (6 items totaling)
  - 🚗 Transport: $150 (3 items)
  - 🛍️ Shopping: $350 (2 items)
  - 🎬 Entertainment: $100 (2 items)
  - 📱 Bills: $200 (2 items)

**Expected Dashboard Display:**
```
Summary Cards:
- Total Spent: $1,100.00
- Total Income: $5,000.00
- Savings: $3,900.00
- Save Rate: 78.0%
- Status: ✓ On Track (green)

Spending by Category (Pie):
- Food: 27.3%
- Shopping: 31.8%
- Bills: 18.2%
- Transport: 13.6%
- Entertainment: 9.1%

Spending by Category (Bar):
- Shopping: $350 (tallest bar)
- Food: $300
- Bills: $200
- Transport: $150
- Entertainment: $100 (shortest bar)

Recent Transactions (shows 10 or fewer):
🛍️  New outfit  shopping • 1 hour ago • Card  $150.00
🍔  Dinner  food • 3 hours ago • Card  $45.00
📱  Internet bill  bills • 1 day ago • Bank Transfer  $50.00
... (more items)
```

## Troubleshooting

### Issue: Dashboard shows "No data available"
**Possible Causes:**
- Backend not running
- API endpoint not working
- User not authenticated
- Database connection issue

**Fix:**
1. Check backend status: `http://localhost:5196/health` (if available)
2. Check browser console for API errors
3. Verify JWT token in localStorage
4. Check MongoDB connection in backend

### Issue: Charts show "No expense data"
**Possible Causes:**
- No expenses in database
- API returns empty categoryBreakdown
- Data not loading

**Fix:**
1. Add test expenses through UI
2. Check Network tab in DevTools
3. Verify API response: Open DevTools → Network → `/api/dashboard/summary`
4. Check if response includes categoryBreakdown array

### Issue: Numbers don't match calculation
**Possible Causes:**
- Calculation error in frontend
- Backend returning wrong values
- Data format issue

**Fix:**
1. Manually verify math: Spent = sum of all expenses
2. Check API response format
3. Verify backend DashboardService logic

### Issue: Charts render but are empty
**Possible Causes:**
- Data format issue
- Color or configuration problem
- Recharts library issue

**Fix:**
1. Check browser console for warnings
2. Verify data in DevTools (React DevTools)
3. Ensure categoryBreakdown has items with non-zero amounts

### Issue: Recent Transactions show wrong data
**Possible Causes:**
- Category as string instead of object (fixed in this update)
- Date formatting issue
- Data mapping error

**Fix:**
1. Verify backend returns ExpenseDto objects
2. Check if category field is string
3. Check formatDate function behavior

## Performance Notes

- Dashboard loads via single API call
- Summary calculation done on backend
- No additional API calls needed
- Charts render efficiently with Recharts
- Data updates when new expenses added

## Browser DevTools Debugging

### Check API Response
1. Open DevTools (F12)
2. Go to Network tab
3. Look for `/api/dashboard/summary` request
4. Check Response tab - verify structure:
   ```json
   {
     "success": true,
     "data": {
       "totalExpenses": 1100,
       "totalIncome": 5000,
       "savings": 3900,
       "categoryBreakdown": [...],
       "recentTransactions": [...]
     }
   }
   ```

### Debug Component State
1. Install [React DevTools](https://react-devtools-tutorial.vercel.app/)
2. Go to Components tab
3. Find DashboardPage component
4. Check props and state
5. Verify summary object structure

### Check Console for Errors
1. Look for red errors in console
2. Check API error messages
3. Verify no TypeScript issues
4. Check for network failures

## Success Criteria

✅ **Dashboard Integration Complete** when:
- Summary cards display correct calculated values
- Pie chart shows all categories with correct percentages
- Bar chart shows correct spending amounts
- Recent transactions display with proper formatting
- Empty states appear when no data
- All responsive designs work correctly
- No errors in browser console
- All calculations match manual verification
