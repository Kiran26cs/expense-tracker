# API Endpoint Mapping

## Authentication Endpoints

### Frontend → Backend Mapping

| Frontend Call | Backend Endpoint | Status | Notes |
|--------------|------------------|--------|-------|
| POST /auth/request-otp | POST /api/Auth/send-otp | ⚠️ MISMATCH | Need to align |
| POST /auth/verify-otp | POST /api/Auth/verify-otp | ✅ MATCH | |
| POST /auth/signup | POST /api/Auth/signup | ✅ MATCH | |
| POST /auth/login | POST /api/Auth/login | ✅ MATCH | |

## Required Fix

The frontend calls `/auth/request-otp` but the backend expects `/auth/send-otp`.

### Option 1: Update Frontend (Recommended)
Change `auth.api.ts` to use `/Auth/send-otp`

### Option 2: Update Backend
Change `AuthController.cs` to use `[HttpPost("request-otp")]`

---

**Action needed**: Align endpoint names for OTP request.
