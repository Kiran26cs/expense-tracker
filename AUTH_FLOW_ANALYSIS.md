# Complete Auth Flow Analysis & Verification Plan

## 📊 Authentication Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                     SIGNUP FLOW                                 │
└─────────────────────────────────────────────────────────────────┘

STEP 1: REQUEST OTP
└─> POST /api/Auth/send-otp
    Body: { email: "user@example.com", phone: null }
    ↓
    Backend: Generate 6-digit OTP → Store in MongoDB → Log to console
    ↓
    Response: { success: true, data: true }

STEP 2: VERIFY OTP
└─> POST /api/Auth/verify-otp
    Body: { email: "user@example.com", phone: null, otp: "123456" }
    ↓
    Backend: Find OTP → Check expiry → Check attempts → Mark verified
    ↓
    Response: { success: true, data: true }

STEP 3: SIGNUP (Create User)
└─> POST /api/Auth/signup?otp=123456
    Body: { 
      email: "user@example.com", 
      phone: null,
      name: "John Doe",
      currency: "USD",
      monthlyIncome: 5000
    }
    ↓
    Backend: 
      1. Verify OTP again (checks Verified flag)
      2. Check user doesn't exist
      3. Create user in DB
      4. Generate JWT token
      5. Generate refresh token
    ↓
    Response: {
      success: true,
      data: {
        token: "eyJhbGc...",
        refreshToken: "...",
        user: { id, email, phone, name, currency, monthlyIncome }
      }
    }

STEP 4: STORE TOKEN & USE
└─> Frontend stores token in localStorage
└─> All subsequent requests include: Authorization: Bearer {token}


┌─────────────────────────────────────────────────────────────────┐
│                     LOGIN FLOW                                  │
└─────────────────────────────────────────────────────────────────┘

STEP 1-2: Same as signup (Send OTP → Verify OTP)

STEP 3: LOGIN (Authenticate User)
└─> POST /api/Auth/login?otp=123456
    Body: { email: "user@example.com", phone: null }
    ↓
    Backend:
      1. Verify OTP again (checks Verified flag)
      2. Find user by email/phone
      3. Generate JWT token
      4. Generate refresh token
    ↓
    Response: Same as signup response
```

---

## 🔍 Component Analysis

### 1. AuthController - ✅ Correct
```csharp
- POST /api/Auth/send-otp      [FromBody] LoginRequest
- POST /api/Auth/verify-otp    [FromBody] VerifyOtpRequest
- POST /api/Auth/signup        [FromBody] SignupRequest + [FromQuery] string otp
- POST /api/Auth/login         [FromBody] LoginRequest + [FromQuery] string otp
```

**Status:** ✅ All endpoints correctly defined

### 2. AuthService - Analysis

#### SendOtpAsync()
```csharp
✅ Generates 6-digit OTP
✅ Stores in MongoDB OtpRecords collection
✅ Sets 5-minute expiry
✅ Deletes existing OTPs for same email/phone
✅ Logs to console (replace with email/SMS)
```

#### VerifyOtpAsync()
```csharp
✅ Finds OTP by email or phone
✅ Checks not expired (ExpiresAt > UtcNow)
✅ Limits attempts to 3
✅ Increments attempts on failure
✅ Marks as verified on success
```

#### SignupAsync()
```csharp
⚠️ ISSUE FOUND: Calls VerifyOtpAsync() which deletes the OTP
   But VerifyOtpAsync marks as verified, doesn't delete
   However: SignupAsync expects to verify OTP that was already verified
   Problem: VerifyOtpAsync looks for Verified flag but SignupAsync calls it again!
```

#### LoginAsync()
```csharp
Same issue as SignupAsync
```

### 3. OtpRecord Entity - ✅ Correct
```csharp
- Email field
- Phone field
- Otp code
- ExpiresAt timestamp
- Attempts counter
- Verified boolean flag
- CreatedAt timestamp
```

### 4. MongoDB Indexes - ✅ Correct
```csharp
✅ Compound index on email + phone
✅ TTL index on ExpiresAt (auto-delete expired)
```

---

## 🐛 Issues Found & Fixes

### ISSUE #1: Double OTP Verification Problem
**Problem:** 
- VerifyOtpAsync marks OTP as verified
- SignupAsync/LoginAsync call VerifyOtpAsync AGAIN with the same OTP
- This second call tries to find and verify same OTP but verification already consumed it

**Current Flow (BROKEN):**
1. Frontend: POST /verify-otp → Backend: Marks OTP as verified ✓
2. Frontend: POST /signup?otp=xxx → Backend: Calls VerifyOtpAsync AGAIN ✗ (Already verified!)

**Solution:** Create separate method for checking if OTP is verified vs verifying it

---

## ✅ Step-by-Step Verification Plan

### PHASE 1: Check MongoDB
```powershell
mongosh
use ExpenseTrackerDB
show collections
# You should see: otpRecords collection exists
```

### PHASE 2: Test Step 1 - Send OTP
**URL:** POST http://localhost:5196/api/Auth/send-otp
**Headers:** Content-Type: application/json
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
# Should return: { email, phone, otp: "XXXXXX", expiresAt, attempts: 0, verified: false }
```

**Check Backend Console:**
```
✓ OTP sent to test@example.com: 123456 (Expires in 5 minutes)
```

### PHASE 3: Test Step 2 - Verify OTP
**URL:** POST http://localhost:5196/api/Auth/verify-otp
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
# Should show: verified: true, attempts: 0
```

### PHASE 4: Test Step 3 - Signup ⚠️ (THIS IS BROKEN)
**URL:** POST http://localhost:5196/api/Auth/signup?otp=123456
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
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "...",
    "user": {
      "id": "...",
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

**What Actually Happens (BUG):**
- Signup calls VerifyOtpAsync AGAIN
- This tries to verify an already-verified OTP
- FAILS because logic is checking if OTP exists AND not expired AND attempts < 3
- But it's trying to verify again when it should just check if it's verified

---

## 🔧 Fix Required

Need to fix AuthService.cs:

```csharp
// ADD NEW METHOD: Check if OTP is already verified (don't verify again)
private async Task<bool> IsOtpVerifiedAsync(string? email, string? phone, string otp)
{
    var filter = Builders<OtpRecord>.Filter.Or(
        Builders<OtpRecord>.Filter.Eq(o => o.Email, email),
        Builders<OtpRecord>.Filter.Eq(o => o.Phone, phone)
    );
    
    var otpRecord = await _context.OtpRecords.Find(filter).FirstOrDefaultAsync();
    
    if (otpRecord == null)
        return false;
    
    // Check OTP code matches
    if (otpRecord.Otp != otp)
        return false;
    
    // Check is marked as verified
    if (!otpRecord.Verified)
        return false;
    
    // Check not expired
    if (DateTime.UtcNow > otpRecord.ExpiresAt)
        return false;
    
    return true;
}

// UPDATE SignupAsync to use IsOtpVerifiedAsync instead of VerifyOtpAsync
public async Task<AuthResponse> SignupAsync(SignupRequest request, string otp)
{
    if (!await IsOtpVerifiedAsync(request.Email, request.Phone, otp))
        throw new UnauthorizedAccessException("Invalid OTP");
    
    // ... rest of code
}

// UPDATE LoginAsync to use IsOtpVerifiedAsync instead of VerifyOtpAsync
public async Task<AuthResponse> LoginAsync(string? email, string? phone, string otp)
{
    if (!await IsOtpVerifiedAsync(email, phone, otp))
        throw new UnauthorizedAccessException("Invalid OTP");
    
    // ... rest of code
}
```

---

## 📋 Complete Verification Checklist

```
BACKEND SETUP:
☐ MongoDB running on localhost:27017
☐ Backend running on localhost:5196
☐ Database: ExpenseTrackerDB created
☐ Collection: otpRecords exists

STEP 1 - SEND OTP:
☐ POST /api/Auth/send-otp returns success: true
☐ OTP record created in MongoDB
☐ ExpiresAt is 5 minutes from now
☐ OTP is 6 digits
☐ Console shows: "✓ OTP sent to..."

STEP 2 - VERIFY OTP:
☐ POST /api/Auth/verify-otp returns success: true
☐ MongoDB record updated with verified: true
☐ Attempts counter is 0

STEP 3 - SIGNUP:
☐ POST /api/Auth/signup?otp=XXXXXX returns success: true
☐ User created in users collection
☐ Token is a valid JWT
☐ RefreshToken is a valid base64 string
☐ User object returned with all fields

STEP 4 - LOGIN:
☐ POST /api/Auth/login?otp=XXXXXX returns success: true
☐ Same token response format as signup

STEP 5 - USE TOKEN:
☐ GET /api/Auth/me with Authorization header works
☐ Token contains user ID in Claims
☐ Without token: returns 401 Unauthorized
```

---

## 🎯 Execution Plan

### You Need To Do:

1. **Apply the fix** (I'll do this in next response)
   - Add `IsOtpVerifiedAsync()` method
   - Update `SignupAsync()` to use new method
   - Update `LoginAsync()` to use new method

2. **Restart backend**
   ```powershell
   dotnet run
   ```

3. **Test with Swagger UI**
   - Go to http://localhost:5196/swagger
   - Test all 4 endpoints in order

4. **Or test with Postman/cURL**
   - Use exact URLs and bodies from Phase 1-5 above

5. **Verify each step in MongoDB**
   - Check OTP record created
   - Check OTP record updated with verified: true
   - Check user created in users collection

---

## 📊 Token Flow

```
SIGNUP RESPONSE:
↓
Frontend stores: localStorage.setItem('authToken', token)
↓
Subsequent requests include:
Headers: {
  'Authorization': 'Bearer eyJhbGc...'
}
↓
Backend extracts userId from JWT claims
↓
Verifies signature with JWT secret
↓
If valid: Request succeeds
If invalid: Returns 401 Unauthorized
```

---

## Summary

| Component | Status | Issue |
|-----------|--------|-------|
| Send OTP | ✅ Working | None |
| Verify OTP | ✅ Working | None |
| Signup | ❌ Broken | Double verification |
| Login | ❌ Broken | Double verification |
| JWT Token | ✅ Working | None |
| Refresh Token | ✅ Working | None |

**Fix Required:** Add `IsOtpVerifiedAsync()` method and use it in Signup/Login
