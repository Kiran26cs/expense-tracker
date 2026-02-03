# Complete Authentication Testing Guide

## 🎯 Quick Test Plan (10 minutes)

Follow these exact steps in order. Copy-paste the values.

---

## ✅ STEP 1: Send OTP

**Tool:** Postman, Swagger UI, or cURL

**Method:** POST
**URL:** `http://localhost:5196/api/Auth/send-otp`

**Headers:**
```
Content-Type: application/json
```

**Body:**
```json
{
  "email": "test@example.com",
  "phone": null
}
```

**Expected Response:**
```json
{
  "success": true,
  "data": true,
  "message": null,
  "errors": null
}
```

**Verify in MongoDB:**
```powershell
mongosh
use ExpenseTrackerDB
db.otpRecords.findOne({ email: "test@example.com" })
```

**Should Show:**
```json
{
  "_id": ObjectId("..."),
  "email": "test@example.com",
  "phone": null,
  "otp": "123456",
  "expiresAt": ISODate("2024-02-03T...:35:00Z"),
  "attempts": 0,
  "verified": false,
  "createdAt": ISODate("2024-02-03T...:30:00Z")
}
```

**Console Output:**
```
✓ OTP sent to test@example.com: 123456 (Expires in 5 minutes)
```

---

## ✅ STEP 2: Verify OTP

**Method:** POST
**URL:** `http://localhost:5196/api/Auth/verify-otp`

**Headers:**
```
Content-Type: application/json
```

**Body:**
```json
{
  "email": "test@example.com",
  "phone": null,
  "otp": "123456"
}
```

**Expected Response:**
```json
{
  "success": true,
  "data": true,
  "message": null,
  "errors": null
}
```

**Verify in MongoDB:**
```powershell
db.otpRecords.findOne({ email: "test@example.com" })
# Should now show: verified: true, attempts: 0
```

---

## ✅ STEP 3: Signup (Create User)

**Method:** POST
**URL:** `http://localhost:5196/api/Auth/signup?otp=123456`

⚠️ **Important:** The OTP code goes in the **query parameter**, not in body!

**Headers:**
```
Content-Type: application/json
```

**Body:**
```json
{
  "email": "test@example.com",
  "phone": null,
  "name": "Test User",
  "currency": "USD",
  "monthlyIncome": 5000
}
```

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI2NWMxMjM0NTY3OGFiY2RlZjAxMjM0NTYiLCJlbWFpbCI6InRlc3RAZXhhbXBsZS5jb20iLCJqdGkiOiJhYmNkZWYwMC0xMjM0LTU2NzgtOWFiYy1kZWYwMDEyMzQ1NjciLCJleHAiOjE3MDcwOTkyMjcsImlzcyI6IkV4cGVuc2VzQmFja2VuZCIsImF1ZCI6IkV4cGVuc2VzQmFja2VuZCJ9.signature",
    "refreshToken": "xK7pL2mN8qZ3wR9tY5vB1cD4eF6gH9jK0lM2nO4pQ6rS8tU0vW2xY3zA5bC6dE7f",
    "user": {
      "id": "65c1234567890abcdef012345",
      "email": "test@example.com",
      "phone": null,
      "name": "Test User",
      "currency": "USD",
      "monthlyIncome": 5000
    }
  },
  "message": null,
  "errors": null
}
```

**Verify in MongoDB:**
```powershell
db.users.findOne({ email: "test@example.com" })
# Should show user created with all fields
```

**Save the Token:**
```
Token: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJz... (copy from response)
```

---

## ✅ STEP 4: Login (Existing User)

Use a **fresh OTP** for login!

**First:** Send and verify another OTP
```powershell
# POST /api/Auth/send-otp with same email
# POST /api/Auth/verify-otp with new OTP code
```

**Then:** Login

**Method:** POST
**URL:** `http://localhost:5196/api/Auth/login?otp=XXXXXX`

**Body:**
```json
{
  "email": "test@example.com",
  "phone": null
}
```

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGc...",
    "refreshToken": "...",
    "user": { ... }
  },
  "message": null,
  "errors": null
}
```

---

## ✅ STEP 5: Use Token to Access Protected Endpoint

**Method:** GET
**URL:** `http://localhost:5196/api/Auth/me`

**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJz...
```

(Replace with token from signup response)

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "id": "65c1234567890abcdef012345",
    "email": "test@example.com",
    "phone": null,
    "name": "Test User",
    "currency": "USD",
    "monthlyIncome": 5000
  },
  "message": null,
  "errors": null
}
```

**Test Without Token:**

**Method:** GET
**URL:** `http://localhost:5196/api/Auth/me`
**Headers:** (empty, no Authorization)

**Expected Response:**
```json
{
  "success": false,
  "data": null,
  "message": "Unauthorized",
  "errors": null
}
```

HTTP Status: 401

---

## 🧪 Using Swagger UI (Easier!)

1. Go to: http://localhost:5196/swagger
2. Expand "Auth" section
3. Click each endpoint in order:
   - "POST /api/Auth/send-otp"
   - "POST /api/Auth/verify-otp"
   - "POST /api/Auth/signup"
   - "POST /api/Auth/login"
   - "GET /api/Auth/me"
4. Fill in the values from above
5. Click "Try it out"

---

## 🧪 Using cURL (Terminal)

### Step 1: Send OTP
```bash
curl -X POST http://localhost:5196/api/Auth/send-otp \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","phone":null}'
```

### Step 2: Verify OTP
```bash
curl -X POST http://localhost:5196/api/Auth/verify-otp \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","phone":null,"otp":"123456"}'
```

### Step 3: Signup
```bash
curl -X POST "http://localhost:5196/api/Auth/signup?otp=123456" \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","phone":null,"name":"Test User","currency":"USD","monthlyIncome":5000}'
```

Save the token from response, then:

### Step 5: Get User Info
```bash
curl -X GET http://localhost:5196/api/Auth/me \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJz..."
```

---

## 📊 Complete Flow - What Happens Behind Scenes

```
USER SIGNUP FLOW:

1. Frontend → Backend: POST /send-otp
   Backend: 
     ├─ Generate 6-digit OTP: "123456"
     ├─ Create OtpRecord in MongoDB:
     │  {
     │    email: "test@example.com",
     │    otp: "123456",
     │    expiresAt: UtcNow + 5min,
     │    verified: false,
     │    attempts: 0
     │  }
     ├─ Delete any existing OTPs for this email
     └─ Log: "✓ OTP sent to test@example.com: 123456"

2. Frontend → Backend: POST /verify-otp
   Backend:
     ├─ Find OTP record by email
     ├─ Check: Not expired? ✓
     ├─ Check: Attempts < 3? ✓
     ├─ Check: OTP code matches? ✓
     ├─ Update record: verified = true
     └─ Return: success: true

3. Frontend → Backend: POST /signup?otp=123456
   Backend:
     ├─ Check: IsOtpVerifiedAsync() = true? ✓
     │  └─ Finds OTP record
     │  └─ Checks: verified = true AND not expired
     ├─ Check: User not already exist? ✓
     ├─ Create User:
     │  {
     │    email: "test@example.com",
     │    name: "Test User",
     │    currency: "USD",
     │    monthlyIncome: 5000,
     │    createdAt: UtcNow
     │  }
     ├─ Insert into MongoDB users collection
     ├─ Generate JWT Token:
     │  Header: { alg: "HS256", typ: "JWT" }
     │  Payload: {
     │    sub: "65c1234...",
     │    email: "test@example.com",
     │    jti: "uuid",
     │    exp: UtcNow + 24hours,
     │    iss: "ExpensesBackend",
     │    aud: "ExpensesBackend"
     │  }
     │  Signature: HMACSHA256(secret)
     ├─ Generate Refresh Token (random 32 bytes base64)
     └─ Return: { token, refreshToken, user }

4. Frontend → Backend: GET /me
   Headers: Authorization: Bearer {token}
   Backend:
     ├─ Extract JWT token from header
     ├─ Verify signature with secret ✓
     ├─ Extract userId from claims: "65c1234..."
     ├─ Fetch user from MongoDB
     └─ Return user data
```

---

## ✅ Verification Checklist

```
Before Testing:
☐ MongoDB running: mongosh connects
☐ Backend running: http://localhost:5196/swagger loads
☐ No errors in backend console

Step 1 - Send OTP:
☐ Response shows success: true
☐ OTP record created in MongoDB
☐ Console shows: "✓ OTP sent to..."
☐ OTP is 6 digits

Step 2 - Verify OTP:
☐ Response shows success: true
☐ MongoDB record updated with verified: true
☐ Same OTP works only once (attempts > 0 on second try)

Step 3 - Signup:
☐ Response shows success: true
☐ Token is in response (starts with "eyJ")
☐ RefreshToken is in response
☐ User data shows in response
☐ User created in MongoDB users collection
☐ User has ID (MongoDB ObjectId)

Step 4 - Login (new OTP):
☐ Same flow as signup
☐ Returns valid token
☐ Same user data returned

Step 5 - Use Token:
☐ GET /me with token returns user data
☐ Without token returns 401 error
☐ Token works multiple times (no expiry in test)

Error Cases:
☐ Invalid OTP: returns error
☐ Expired OTP (>5 min): returns error
☐ Wrong email in verify: returns error
☐ Non-existent user in login: returns error
☐ Duplicate email in signup: returns error
```

---

## 🚨 Troubleshooting

### Issue: "User already exists"
- **Cause:** User created in step 3, trying signup again
- **Fix:** Use different email for second test
- **Or:** Delete user from MongoDB: `db.users.deleteOne({ email: "test@example.com" })`

### Issue: "Invalid OTP"
- **Cause:** Wrong OTP code or not verified first
- **Fix:** 
  1. Send OTP first (step 1)
  2. Copy exact OTP from console
  3. Verify it (step 2)
  4. Use in signup (step 3)

### Issue: "OTP expired"
- **Cause:** More than 5 minutes elapsed
- **Fix:** Send new OTP (only 5-minute window)

### Issue: "Invalid or expired OTP. Please verify OTP first."
- **Cause:** Using verify-otp OTP code in signup but didn't verify first
- **Fix:** Always do: send-otp → verify-otp → signup

### Issue: Token doesn't work for /me
- **Cause:** Wrong Authorization header format
- **Fix:** Should be: `Authorization: Bearer eyJhbGc...` (with "Bearer " prefix)

---

## 📈 Full Workflow Summary

| Step | Method | URL | Query Param | Purpose |
|------|--------|-----|-------------|---------|
| 1 | POST | /Auth/send-otp | - | Generate OTP |
| 2 | POST | /Auth/verify-otp | - | Mark OTP verified |
| 3 | POST | /Auth/signup | otp=XXXXXX | Create user, get token |
| 4 | POST | /Auth/login | otp=XXXXXX | Login user, get token |
| 5 | GET | /Auth/me | - | Get user info (needs token) |

---

**All systems ready! Follow the steps above to test the complete auth flow.** ✅
