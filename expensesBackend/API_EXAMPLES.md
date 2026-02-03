# API Examples - Expenses Backend

This document provides practical examples of how to use the Expenses Backend API.

## Base URL

```
http://localhost:5196/api
```

## Authentication Flow Example

### 1. Send OTP

**Request:**
```http
POST /api/auth/send-otp
Content-Type: application/json

{
  "email": "user@example.com"
}
```

**Response:**
```json
{
  "success": true,
  "data": true,
  "error": null
}
```

**Console Output:**
```
OTP for user@example.com: 123456
```

### 2. Verify OTP (Optional)

**Request:**
```http
POST /api/auth/verify-otp
Content-Type: application/json

{
  "email": "user@example.com",
  "otp": "123456"
}
```

**Response:**
```json
{
  "success": true,
  "data": true,
  "error": null
}
```

### 3. Signup (First Time Users)

**Request:**
```http
POST /api/auth/signup?otp=123456
Content-Type: application/json

{
  "email": "user@example.com",
  "name": "John Doe",
  "currency": "USD",
  "monthlyIncome": 5000
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "abc123...",
    "user": {
      "id": "65f1234567890abcdef12345",
      "email": "user@example.com",
      "phone": null,
      "name": "John Doe",
      "currency": "USD",
      "monthlyIncome": 5000
    }
  },
  "error": null
}
```

### 4. Login (Existing Users)

**Request:**
```http
POST /api/auth/login?otp=123456
Content-Type: application/json

{
  "email": "user@example.com"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "xyz789...",
    "user": {
      "id": "65f1234567890abcdef12345",
      "email": "user@example.com",
      "name": "John Doe",
      "currency": "USD",
      "monthlyIncome": 5000
    }
  },
  "error": null
}
```

## Expense Management Examples

### Create Expense

**Request:**
```http
POST /api/expenses
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "amount": 45.99,
  "date": "2026-02-02T10:30:00Z",
  "category": "Food & Dining",
  "paymentMethod": "Credit Card",
  "description": "Lunch at Restaurant",
  "notes": "Team lunch meeting",
  "isRecurring": false
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "65f9876543210fedcba09876",
    "amount": 45.99,
    "date": "2026-02-02T10:30:00Z",
    "category": "Food & Dining",
    "paymentMethod": "Credit Card",
    "description": "Lunch at Restaurant",
    "notes": "Team lunch meeting",
    "receiptUrl": null,
    "isRecurring": false,
    "createdAt": "2026-02-02T10:35:00Z"
  },
  "error": null
}
```

### Create Recurring Expense

**Request:**
```http
POST /api/expenses
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "amount": 99.99,
  "date": "2026-02-01T00:00:00Z",
  "category": "Subscriptions",
  "paymentMethod": "Credit Card",
  "description": "Netflix Subscription",
  "isRecurring": true,
  "recurringConfig": {
    "frequency": "monthly",
    "startDate": "2026-02-01T00:00:00Z",
    "endDate": null
  }
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "65f1111111111111111111111",
    "amount": 99.99,
    "date": "2026-02-01T00:00:00Z",
    "category": "Subscriptions",
    "paymentMethod": "Credit Card",
    "description": "Netflix Subscription",
    "notes": null,
    "receiptUrl": null,
    "isRecurring": true,
    "createdAt": "2026-02-02T11:00:00Z"
  },
  "error": null
}
```

### Get All Expenses

**Request:**
```http
GET /api/expenses
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": "65f9876543210fedcba09876",
      "amount": 45.99,
      "date": "2026-02-02T10:30:00Z",
      "category": "Food & Dining",
      "paymentMethod": "Credit Card",
      "description": "Lunch at Restaurant",
      "notes": "Team lunch meeting",
      "receiptUrl": null,
      "isRecurring": false,
      "createdAt": "2026-02-02T10:35:00Z"
    }
  ],
  "error": null
}
```

### Get Expenses with Filters

**Request:**
```http
GET /api/expenses?startDate=2026-02-01&endDate=2026-02-28&category=Food%20%26%20Dining
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Update Expense

**Request:**
```http
PUT /api/expenses/65f9876543210fedcba09876
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "amount": 50.00,
  "notes": "Updated notes"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "65f9876543210fedcba09876",
    "amount": 50.00,
    "date": "2026-02-02T10:30:00Z",
    "category": "Food & Dining",
    "paymentMethod": "Credit Card",
    "description": "Lunch at Restaurant",
    "notes": "Updated notes",
    "receiptUrl": null,
    "isRecurring": false,
    "createdAt": "2026-02-02T10:35:00Z"
  },
  "error": null
}
```

### Delete Expense

**Request:**
```http
DELETE /api/expenses/65f9876543210fedcba09876
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response:**
```json
{
  "success": true,
  "data": true,
  "error": null
}
```

### Upload Receipt

**Request:**
```http
POST /api/expenses/65f9876543210fedcba09876/receipt
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: multipart/form-data

file: [binary data]
```

**Response:**
```json
{
  "success": true,
  "data": "/uploads/receipts/65f9876543210fedcba09876_receipt.jpg",
  "error": null
}
```

## Dashboard Examples

### Get Dashboard Summary

**Request:**
```http
GET /api/dashboard/summary
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response:**
```json
{
  "success": true,
  "data": {
    "totalExpenses": 1245.99,
    "totalIncome": 5000.00,
    "savings": 3754.01,
    "categoryBreakdown": [
      {
        "category": "Food & Dining",
        "amount": 450.00,
        "percentage": 36.1
      },
      {
        "category": "Transportation",
        "amount": 300.00,
        "percentage": 24.1
      },
      {
        "category": "Entertainment",
        "amount": 200.00,
        "percentage": 16.1
      }
    ],
    "recentTransactions": [
      {
        "id": "65f9876543210fedcba09876",
        "amount": 45.99,
        "date": "2026-02-02T10:30:00Z",
        "category": "Food & Dining",
        "paymentMethod": "Credit Card",
        "description": "Lunch at Restaurant",
        "notes": "Team lunch meeting",
        "receiptUrl": null,
        "isRecurring": false,
        "createdAt": "2026-02-02T10:35:00Z"
      }
    ]
  },
  "error": null
}
```

### Get Dashboard Summary with Date Range

**Request:**
```http
GET /api/dashboard/summary?startDate=2026-01-01&endDate=2026-01-31
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Get Monthly Trends

**Request:**
```http
GET /api/dashboard/trends?months=6
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "month": "2025-09",
      "totalExpenses": 1150.00,
      "totalIncome": 5000.00
    },
    {
      "month": "2025-10",
      "totalExpenses": 1300.00,
      "totalIncome": 5000.00
    },
    {
      "month": "2025-11",
      "totalExpenses": 1450.00,
      "totalIncome": 5000.00
    }
  ],
  "error": null
}
```

## Error Responses

### Invalid OTP

**Response:**
```json
{
  "success": false,
  "data": null,
  "error": "Invalid OTP"
}
```

### Unauthorized

**Response:**
```json
{
  "success": false,
  "data": null,
  "error": "User not authenticated"
}
```

**Status Code:** 401

### Not Found

**Response:**
```json
{
  "success": false,
  "data": null,
  "error": "Expense not found"
}
```

**Status Code:** 404

### Validation Error

**Response:**
```json
{
  "success": false,
  "data": null,
  "error": "Amount is required"
}
```

**Status Code:** 400

## PowerShell Examples

### Complete Authentication Flow

```powershell
# 1. Send OTP
$response = Invoke-RestMethod -Uri "http://localhost:5196/api/auth/send-otp" `
    -Method POST `
    -ContentType "application/json" `
    -Body '{"email":"test@example.com"}'

# 2. Check console for OTP (e.g., 123456)

# 3. Signup or Login
$authResponse = Invoke-RestMethod -Uri "http://localhost:5196/api/auth/signup?otp=123456" `
    -Method POST `
    -ContentType "application/json" `
    -Body '{"email":"test@example.com","name":"Test User","currency":"USD","monthlyIncome":5000}'

# 4. Save token
$token = $authResponse.data.token
Write-Host "Token: $token"
```

### Create and Manage Expenses

```powershell
# Set up headers
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Create expense
$expenseData = @{
    amount = 45.99
    date = (Get-Date).ToString("o")
    category = "Food & Dining"
    paymentMethod = "Credit Card"
    description = "Lunch"
} | ConvertTo-Json

$newExpense = Invoke-RestMethod -Uri "http://localhost:5196/api/expenses" `
    -Method POST `
    -Headers $headers `
    -Body $expenseData

Write-Host "Created expense ID: $($newExpense.data.id)"

# Get all expenses
$expenses = Invoke-RestMethod -Uri "http://localhost:5196/api/expenses" `
    -Method GET `
    -Headers $headers

Write-Host "Total expenses: $($expenses.data.Count)"

# Get dashboard
$dashboard = Invoke-RestMethod -Uri "http://localhost:5196/api/dashboard/summary" `
    -Method GET `
    -Headers $headers

Write-Host "Total Expenses: $($dashboard.data.totalExpenses)"
Write-Host "Total Income: $($dashboard.data.totalIncome)"
Write-Host "Savings: $($dashboard.data.savings)"
```

## cURL Examples

### Authentication

```bash
# Send OTP
curl -X POST http://localhost:5196/api/auth/send-otp \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com"}'

# Login
curl -X POST "http://localhost:5196/api/auth/login?otp=123456" \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com"}'
```

### Expenses

```bash
# Create expense
curl -X POST http://localhost:5196/api/expenses \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 45.99,
    "date": "2026-02-02T10:30:00Z",
    "category": "Food & Dining",
    "paymentMethod": "Credit Card",
    "description": "Lunch"
  }'

# Get expenses
curl -X GET http://localhost:5196/api/expenses \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## Testing Tips

1. **Use Swagger UI** for quick testing: http://localhost:5196/swagger
2. **Check Console Output** for OTP codes during development
3. **Save JWT Token** to reuse in subsequent requests
4. **MongoDB Compass** to view database directly
5. **Postman Collection** - Consider importing these examples into Postman

## Rate Limiting (TODO)

Currently no rate limiting. In production, implement:
- 5 OTP requests per 15 minutes per email/phone
- 100 API requests per minute per user
- IP-based throttling for authentication endpoints
