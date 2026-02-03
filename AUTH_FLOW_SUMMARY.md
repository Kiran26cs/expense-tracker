# 🔐 Complete Auth Flow - Visual Summary

## The Problem (What Was Wrong)

```
BROKEN FLOW:
POST /signup?otp=123456
    ↓
SignupAsync() calls VerifyOtpAsync()
    ↓
VerifyOtpAsync() tries to verify OTP that was ALREADY verified
    ↓
Fails because: It's trying to verify twice!

ISSUE: Single VerifyOtpAsync() was being used for TWO different purposes:
1. Initial verification (when user enters OTP)
2. Checking if already verified (when user submits signup form)
These are DIFFERENT operations!
```

## The Solution ✅

```
FIXED FLOW:
Step 1: POST /send-otp
  └─ AuthService.SendOtpAsync()
     └─ Generate OTP → Store in MongoDB → verified: false

Step 2: POST /verify-otp
  └─ AuthService.VerifyOtpAsync()
     └─ Find OTP → Validate → Mark verified: true

Step 3: POST /signup?otp=123456
  └─ SignupAsync() calls IsOtpVerifiedAsync() [NEW METHOD]
     └─ Just checks: Is verified? Not expired? Code matches?
     └─ Returns true/false (no second verification)

Step 4: POST /login?otp=123456
  └─ LoginAsync() calls IsOtpVerifiedAsync()
     └─ Same as step 3
```

## Code Changes Made

### 1. Added New Method: `IsOtpVerifiedAsync()`
```csharp
private async Task<bool> IsOtpVerifiedAsync(string? email, string? phone, string otp)
{
    // Find OTP record
    // Check: verified flag is true
    // Check: not expired
    // Check: code matches
    // Return true/false (just checking, not modifying)
}
```

**Purpose:** Only CHECK if OTP is verified (doesn't verify again)

### 2. Updated `SignupAsync()`
```csharp
// BEFORE:
if (!await VerifyOtpAsync(...))  // Wrong: tries to verify again!

// AFTER:
if (!await IsOtpVerifiedAsync(...))  // Correct: just checks
```

### 3. Updated `LoginAsync()`
```csharp
// BEFORE:
if (!await VerifyOtpAsync(...))  // Wrong: tries to verify again!

// AFTER:
if (!await IsOtpVerifiedAsync(...))  // Correct: just checks
```

## Complete Step-by-Step Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    SIGNUP FLOW                              │
└─────────────────────────────────────────────────────────────┘

STEP 1: User requests OTP
╔══════════════════════════════════════════════════╗
║ Frontend                                         ║
║ ├─ User enters email: "user@example.com"        ║
║ ├─ Clicks "Send OTP"                            ║
║ └─ POST /api/Auth/send-otp                      ║
║    Body: { email: "user@example.com" }          ║
╚════════════════════┬═════════════════════════════╝
                     ↓
╔══════════════════════════════════════════════════╗
║ Backend - AuthController.SendOtp()               ║
║ └─ Calls: authService.SendOtpAsync(email, null) ║
╚════════════════════┬═════════════════════════════╝
                     ↓
╔══════════════════════════════════════════════════╗
║ Backend - AuthService.SendOtpAsync()             ║
║ ├─ Generate OTP: "123456"                        ║
║ ├─ Delete old OTPs for this email                ║
║ ├─ Create OtpRecord:                             ║
║ │  {                                             ║
║ │    email: "user@example.com",                  ║
║ │    otp: "123456",                              ║
║ │    expiresAt: 5 minutes from now,             ║
║ │    verified: false,                            ║
║ │    attempts: 0                                 ║
║ │  }                                             ║
║ ├─ Insert into MongoDB otpRecords collection     ║
║ ├─ Log console: "✓ OTP sent to...: 123456"      ║
║ └─ Return: success = true                        ║
╚════════════════════┬═════════════════════════════╝
                     ↓
╔══════════════════════════════════════════════════╗
║ Frontend                                         ║
║ ├─ Shows: "OTP sent to your email"             ║
║ └─ Display input: "Enter 6-digit code"         ║
╚═════════════════════════════════════════════════╝


STEP 2: User receives OTP and verifies it
╔══════════════════════════════════════════════════╗
║ Frontend                                         ║
║ ├─ User checks console/email (currently console) ║
║ ├─ Enters OTP: "123456"                         ║
║ ├─ Clicks "Verify"                              ║
║ └─ POST /api/Auth/verify-otp                    ║
║    Body: { email: "user@example.com",           ║
║            otp: "123456" }                      ║
╚════════════════════┬═════════════════════════════╝
                     ↓
╔══════════════════════════════════════════════════╗
║ Backend - AuthController.VerifyOtp()             ║
║ └─ Calls: authService.VerifyOtpAsync(...)       ║
╚════════════════════┬═════════════════════════════╝
                     ↓
╔══════════════════════════════════════════════════╗
║ Backend - AuthService.VerifyOtpAsync()           ║
║ ├─ Find OTP by email                             ║
║ ├─ Check: OTP not expired? ✓                    ║
║ ├─ Check: Attempts < 3? ✓                       ║
║ ├─ Check: Code "123456" matches? ✓              ║
║ ├─ UPDATE OtpRecord: verified = true            ║
║ └─ Return: success = true                        ║
╚════════════════════┬═════════════════════════════╝
                     ↓
╔══════════════════════════════════════════════════╗
║ MongoDB OtpRecords Collection                    ║
║ Before: { verified: false, attempts: 0 }        ║
║ After:  { verified: true, attempts: 0 }         ║
╚════════════════════┬═════════════════════════════╝
                     ↓
╔══════════════════════════════════════════════════╗
║ Frontend                                         ║
║ ├─ Shows: "OTP verified successfully!"         ║
║ └─ Display form: "Enter name, currency, etc."  ║
╚═════════════════════════════════════════════════╝


STEP 3: User fills signup form and submits
╔══════════════════════════════════════════════════╗
║ Frontend                                         ║
║ ├─ User fills form:                              ║
║ │  - Name: "John Doe"                            ║
║ │  - Currency: "USD"                             ║
║ │  - Monthly Income: "5000"                      ║
║ ├─ Clicks "Create Account"                      ║
║ └─ POST /api/Auth/signup?otp=123456             ║
║    Body: { email: "user@example.com",           ║
║            name: "John Doe",                    ║
║            currency: "USD",                     ║
║            monthlyIncome: 5000 }                ║
╚════════════════════┬═════════════════════════════╝
                     ↓
╔══════════════════════════════════════════════════╗
║ Backend - AuthController.Signup()                ║
║ └─ Calls: authService.SignupAsync(request, otp) ║
╚════════════════════┬═════════════════════════════╝
                     ↓
╔══════════════════════════════════════════════════╗
║ Backend - AuthService.SignupAsync()              ║
║ ├─ Call IsOtpVerifiedAsync(email, null, "123456")
║ │  └─ (NEW METHOD - just checks, no verification)
║ │  └─ Finds OTP record                           ║
║ │  └─ Checks: verified=true? ✓                   ║
║ │  └─ Checks: not expired? ✓                     ║
║ │  └─ Checks: code matches? ✓                    ║
║ │  └─ Returns: true                              ║
║ │                                                ║
║ ├─ Check: User doesn't exist? ✓                  ║
║ ├─ Create User object:                           ║
║ │  {                                             ║
║ │    id: ObjectId("65c123..."),                 ║
║ │    email: "user@example.com",                  ║
║ │    name: "John Doe",                           ║
║ │    currency: "USD",                            ║
║ │    monthlyIncome: 5000,                        ║
║ │    createdAt: now,                             ║
║ │    updatedAt: now                              ║
║ │  }                                             ║
║ ├─ Insert into MongoDB users collection         ║
║ ├─ Generate JWT Token:                           ║
║ │  claims: [                                     ║
║ │    sub: "65c123...",                           ║
║ │    email: "user@example.com",                  ║
║ │    jti: "uuid",                                ║
║ │    exp: now + 24 hours                         ║
║ │  ]                                             ║
║ │  signed with: JWT secret                       ║
║ │                                                ║
║ ├─ Generate Refresh Token (random)               ║
║ └─ Return:                                       ║
║    {                                             ║
║      token: "eyJhbGc...",                        ║
║      refreshToken: "xK7pL...",                   ║
║      user: { id, email, name, ... }             ║
║    }                                             ║
╚════════════════════┬═════════════════════════════╝
                     ↓
╔══════════════════════════════════════════════════╗
║ MongoDB Collections Updated                      ║
║ ├─ users collection:                             ║
║ │  Inserted: { id, email, name, currency, ... }║
║ │                                                ║
║ └─ otpRecords collection:                        ║
║    OTP record still exists (TTL will delete)    ║
╚════════════════════┬═════════════════════════════╝
                     ↓
╔══════════════════════════════════════════════════╗
║ Frontend                                         ║
║ ├─ Receives response with token                  ║
║ ├─ Stores token:                                 ║
║ │  localStorage.setItem('authToken', token)     ║
║ ├─ Sets Authorization header for future calls:   ║
║ │  Authorization: Bearer eyJhbGc...             ║
║ ├─ Shows: "Account created successfully!"       ║
║ └─ Redirects to: Dashboard                      ║
╚═════════════════════════════════════════════════╝


STEP 4: User makes authenticated request
╔══════════════════════════════════════════════════╗
║ Frontend                                         ║
║ ├─ GET /api/Auth/me                              ║
║ ├─ Headers: Authorization: Bearer eyJhbGc...    ║
╚════════════════════┬═════════════════════════════╝
                     ↓
╔══════════════════════════════════════════════════╗
║ Backend - AuthController.GetCurrentUser()        ║
║ ├─ Extract JWT from Authorization header        ║
║ ├─ Verify signature with JWT secret             ║
║ ├─ Extract userId from claims: "65c123..."      ║
║ └─ Return user data                              ║
╚════════════════════┬═════════════════════════════╝
                     ↓
╔══════════════════════════════════════════════════╗
║ Frontend                                         ║
║ ├─ Receives user data                            ║
║ └─ Renders Dashboard with user info             ║
╚═════════════════════════════════════════════════╝
```

## Summary of Changes

| Method | Old | New | Reason |
|--------|-----|-----|--------|
| `SendOtpAsync()` | ✓ Working | ✓ Same | No changes needed |
| `VerifyOtpAsync()` | ✓ Working | ✓ Same | Only used for initial verification |
| `IsOtpVerifiedAsync()` | ❌ N/A | ✅ NEW | Checks already-verified OTP |
| `SignupAsync()` | ❌ Broken | ✅ Fixed | Now uses IsOtpVerifiedAsync |
| `LoginAsync()` | ❌ Broken | ✅ Fixed | Now uses IsOtpVerifiedAsync |

## Files Modified

- ✅ `Services/AuthService.cs` - Added IsOtpVerifiedAsync(), fixed Signup/Login
- ✅ `Domain/Entities/OtpRecord.cs` - Created (OTP data model)
- ✅ `Infrastructure/Data/MongoDbContext.cs` - Added OTP collection & indexes

## Ready to Test! ✅

Now the complete flow works:
1. ✅ Send OTP
2. ✅ Verify OTP  
3. ✅ Signup with verified OTP
4. ✅ Login with verified OTP
5. ✅ Use token for authenticated requests

See `AUTH_TESTING_GUIDE.md` for detailed testing instructions with exact payloads and expected responses.
