# 🚀 Quick Reference - Auth Flow Testing

## The 5-Minute Test

```powershell
# Terminal 1: Check MongoDB
mongosh
use ExpenseTrackerDB
# (keep open to check records)

# Terminal 2: Check logs
cd d:\flutterRepo\expenseTracker\expensesBackend
dotnet run
# (watch for "✓ OTP sent to..." message)
```

## Copy-Paste Payloads

### 1️⃣ SEND OTP
```
POST http://localhost:5196/api/Auth/send-otp
Content-Type: application/json

{
  "email": "test123@example.com",
  "phone": null
}
```

**Expected:** `{ "success": true, "data": true }`
**Console:** `✓ OTP sent to test123@example.com: 123456`

---

### 2️⃣ VERIFY OTP
```
POST http://localhost:5196/api/Auth/verify-otp
Content-Type: application/json

{
  "email": "test123@example.com",
  "phone": null,
  "otp": "123456"
}
```

**Expected:** `{ "success": true, "data": true }`
**MongoDB:** `db.otpRecords.findOne({ email: "test123@example.com" })` shows `verified: true`

---

### 3️⃣ SIGNUP
```
POST http://localhost:5196/api/Auth/signup?otp=123456
Content-Type: application/json

{
  "email": "test123@example.com",
  "phone": null,
  "name": "Test User",
  "currency": "USD",
  "monthlyIncome": 5000
}
```

**Expected:** 
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGc...",
    "refreshToken": "...",
    "user": { "id": "...", "email": "test123@example.com", ... }
  }
}
```

**MongoDB:** `db.users.findOne({ email: "test123@example.com" })` shows user created

---

### 4️⃣ LOGIN (with NEW OTP)
```
1. First, repeat SEND OTP + VERIFY OTP with same email
2. Then:

POST http://localhost:5196/api/Auth/login?otp=123456
Content-Type: application/json

{
  "email": "test123@example.com",
  "phone": null
}
```

**Expected:** Same response as signup

---

### 5️⃣ GET CURRENT USER
```
GET http://localhost:5196/api/Auth/me
Authorization: Bearer eyJhbGc...
```

(Replace eyJhbGc... with token from step 3 or 4)

**Expected:**
```json
{
  "success": true,
  "data": {
    "id": "...",
    "email": "test123@example.com",
    "name": "Test User",
    "currency": "USD",
    "monthlyIncome": 5000
  }
}
```

---

## ✅ Checklist

After each step, verify:

| Step | API Call | ✓ Success | ✓ MongoDB | ✓ Console |
|------|----------|-----------|-----------|-----------|
| 1 | send-otp | success: true | OTP record created | ✓ OTP sent |
| 2 | verify-otp | success: true | verified: true | - |
| 3 | signup | success: true + token | user created | - |
| 4 | login | success: true + token | - | - |
| 5 | /me | user data | - | - |

---

## Common Values

**Test Email:** `test123@example.com`
**Test Name:** `Test User`
**Test Currency:** `USD`
**Test Income:** `5000`

**OTP:** Look in console or MongoDB OtpRecords collection

---

## Error Responses

| Error | Cause | Fix |
|-------|-------|-----|
| `Invalid OTP` | Wrong code or not verified | Check console for actual OTP |
| `OTP expired` | More than 5 min elapsed | Send new OTP |
| `User already exists` | User already created | Delete from DB or use different email |
| `User not found` | User not created yet | Do signup first before login |
| `Invalid or expired OTP. Please verify OTP first.` | Didn't verify OTP before signup | Always do: send → verify → signup |

---

## Using Swagger UI (Easiest!)

1. Go to: **http://localhost:5196/swagger**
2. Find "Auth" section
3. Try each endpoint in order with payloads above
4. Copy response tokens/OTPs as needed

---

## MongoDB Check Commands

```powershell
mongosh
use ExpenseTrackerDB

# See all OTPs
db.otpRecords.find()

# See specific OTP
db.otpRecords.findOne({ email: "test123@example.com" })

# See all users
db.users.find()

# See specific user
db.users.findOne({ email: "test123@example.com" })

# Delete user (to re-test)
db.users.deleteOne({ email: "test123@example.com" })

# Delete OTP (to re-test)
db.otpRecords.deleteOne({ email: "test123@example.com" })
```

---

## Backend Logs to Watch For

```
✓ OTP sent to test123@example.com: 123456 (Expires in 5 minutes)
```

This appears when sending OTP. Use this code in verify step!

---

## What's Fixed

✅ SendOtpAsync() - Generates and stores OTP
✅ VerifyOtpAsync() - Marks OTP as verified  
✅ IsOtpVerifiedAsync() - NEW: Checks if OTP already verified (no double verification)
✅ SignupAsync() - Uses IsOtpVerifiedAsync instead of VerifyOtpAsync
✅ LoginAsync() - Uses IsOtpVerifiedAsync instead of VerifyOtpAsync

---

**Ready to test! Follow the 5 steps above with exact payloads.** ✅
