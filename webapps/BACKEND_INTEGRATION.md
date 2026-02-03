# Backend Integration Checklist

This checklist helps you connect the frontend to your backend API.

## ✅ Setup Steps

### 1. Environment Configuration

- [ ] Copy `.env.example` to `.env`
- [ ] Update `VITE_API_BASE_URL` with your backend URL
- [ ] Ensure backend CORS allows your frontend origin

```env
VITE_API_BASE_URL=https://your-backend-api.com/api
```

### 2. API Endpoints Verification

Verify your backend implements these endpoints:

#### Authentication (`/api/auth/`)

- [ ] `POST /request-otp` - Request OTP code
  - Body: `{ emailOrPhone: string }`
  - Response: `{ success: boolean, data: { otpSent: boolean } }`

- [ ] `POST /login` - Login with OTP
  - Body: `{ emailOrPhone: string, otp: string }`
  - Response: `{ success: boolean, data: { user: User, token: string } }`

- [ ] `POST /signup` - User registration
  - Body: `{ name: string, email?: string, phone?: string }`
  - Response: `{ success: boolean, data: { user: User, token: string } }`

- [ ] `POST /verify-otp` - Verify OTP
  - Body: `{ emailOrPhone: string, otp: string }`
  - Response: `{ success: boolean, data: { verified: boolean } }`

- [ ] `GET /me` - Get current user
  - Headers: `Authorization: Bearer {token}`
  - Response: `{ success: boolean, data: User }`

- [ ] `POST /logout` - Logout
  - Headers: `Authorization: Bearer {token}`
  - Response: `{ success: boolean }`

#### Dashboard (`/api/dashboard`)

- [ ] `GET /` - Get dashboard summary
  - Headers: `Authorization: Bearer {token}`
  - Response: `{ success: boolean, data: DashboardSummary }`

#### Expenses (`/api/expenses`)

- [ ] `GET /` - List expenses with pagination and filters
  - Query params: `page, pageSize, startDate, endDate, categories, paymentMethods, minAmount, maxAmount, q`
  - Response: `{ success: boolean, data: PaginatedResponse<Expense> }`

- [ ] `GET /:id` - Get single expense
  - Response: `{ success: boolean, data: Expense }`

- [ ] `POST /` - Create expense
  - Body: `Expense` (without id, createdAt, updatedAt)
  - Response: `{ success: boolean, data: Expense }`

- [ ] `PUT /:id` - Update expense
  - Body: `Partial<Expense>`
  - Response: `{ success: boolean, data: Expense }`

- [ ] `DELETE /:id` - Delete expense
  - Response: `{ success: boolean }`

- [ ] `GET /recurring` - Get recurring expenses
  - Response: `{ success: boolean, data: Expense[] }`

#### Budgets (`/api/budgets`)

- [ ] `GET /` - List budgets
  - Query params: `month` (YYYY-MM format, optional)
  - Response: `{ success: boolean, data: Budget[] }`

- [ ] `GET /:id` - Get single budget
  - Response: `{ success: boolean, data: Budget }`

- [ ] `POST /` - Create budget
  - Body: `{ categoryId, plannedAmount, month }`
  - Response: `{ success: boolean, data: Budget }`

- [ ] `PUT /:id` - Update budget
  - Body: `Partial<Budget>`
  - Response: `{ success: boolean, data: Budget }`

- [ ] `DELETE /:id` - Delete budget
  - Response: `{ success: boolean }`

#### Forecast (`/api/forecast`)

- [ ] `GET /` - Get cash forecast
  - Query params: `months` (default: 6)
  - Response: `{ success: boolean, data: CashForecast }`

- [ ] `POST /simulate` - Simulate purchase impact
  - Body: `{ amount, categoryId, plannedDate }`
  - Response: `{ success: boolean, data: PurchaseSimulation }`

#### Settings (`/api/settings`)

- [ ] `GET /` - Get settings
  - Response: `{ success: boolean, data: Settings }`

- [ ] `PUT /` - Update settings
  - Body: `Partial<Settings>`
  - Response: `{ success: boolean, data: Settings }`

- [ ] `GET /categories` - Get categories
  - Response: `{ success: boolean, data: Category[] }`

- [ ] `POST /categories` - Create category
  - Body: `{ name, icon, color }`
  - Response: `{ success: boolean, data: Category }`

- [ ] `PUT /categories/:id` - Update category
  - Body: `Partial<Category>`
  - Response: `{ success: boolean, data: Category }`

- [ ] `DELETE /categories/:id` - Delete category
  - Response: `{ success: boolean }`

- [ ] `GET /payment-methods` - Get payment methods
  - Response: `{ success: boolean, data: PaymentMethod[] }`

#### Import (`/api/import`)

- [ ] `POST /preview` - Preview CSV import
  - Body: FormData with `file` field
  - Response: `{ success: boolean, data: ImportPreview }`

- [ ] `POST /confirm` - Confirm import
  - Body: `{ expenses: Array }`
  - Response: `{ success: boolean, data: { imported: number, failed: number } }`

### 3. Response Format Verification

All API responses should follow this format:

```typescript
{
  success: boolean;
  data?: T;
  error?: string;
  message?: string;
}
```

**Success Response:**
```json
{
  "success": true,
  "data": { ... }
}
```

**Error Response:**
```json
{
  "success": false,
  "error": "Error message here"
}
```

### 4. CORS Configuration

Your backend must allow:

- [ ] Origin: `http://localhost:3000` (development)
- [ ] Origin: `https://your-frontend-domain.com` (production)
- [ ] Methods: `GET, POST, PUT, DELETE, OPTIONS`
- [ ] Headers: `Content-Type, Authorization`
- [ ] Credentials: `true` (if using cookies)

Example Express.js CORS setup:

```javascript
const cors = require('cors');

app.use(cors({
  origin: process.env.FRONTEND_URL || 'http://localhost:3000',
  credentials: true,
  methods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS'],
  allowedHeaders: ['Content-Type', 'Authorization']
}));
```

### 5. Authentication Flow

- [ ] Backend generates 6-digit OTP on `/request-otp`
- [ ] OTP sent via email/SMS to user
- [ ] Backend validates OTP on `/login` or `/verify-otp`
- [ ] Backend returns JWT token on successful authentication
- [ ] Token includes user ID and expiry time
- [ ] Frontend stores token in localStorage
- [ ] Frontend sends token in Authorization header for protected routes
- [ ] Backend validates token on each protected endpoint

### 6. Data Type Verification

Ensure backend data types match TypeScript types in `src/types/index.ts`:

- [ ] User type
- [ ] Expense type
- [ ] Category type
- [ ] PaymentMethod type
- [ ] Budget type
- [ ] DashboardSummary type
- [ ] CashForecast type
- [ ] PurchaseSimulation type
- [ ] Settings type

### 7. Testing Endpoints

Use this curl command template to test endpoints:

```bash
# Test login
curl -X POST http://localhost:8000/api/auth/request-otp \
  -H "Content-Type: application/json" \
  -d '{"emailOrPhone": "test@example.com"}'

# Test protected endpoint
curl -X GET http://localhost:8000/api/dashboard \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

Or use tools like:
- Postman
- Insomnia
- Thunder Client (VS Code extension)

### 8. Error Handling

Backend should return appropriate HTTP status codes:

- [ ] 200 - Success
- [ ] 201 - Created
- [ ] 400 - Bad Request (validation errors)
- [ ] 401 - Unauthorized (invalid/missing token)
- [ ] 403 - Forbidden (insufficient permissions)
- [ ] 404 - Not Found
- [ ] 500 - Internal Server Error

### 9. Security Checklist

- [ ] Use HTTPS in production
- [ ] Implement rate limiting (e.g., max 5 OTP requests per hour)
- [ ] Sanitize user inputs
- [ ] Use environment variables for secrets
- [ ] Implement CSRF protection
- [ ] Set secure cookie flags if using cookies
- [ ] Implement proper password hashing (if adding password auth)
- [ ] Add request logging
- [ ] Implement API request validation

### 10. Performance Optimization

- [ ] Implement pagination for list endpoints
- [ ] Add caching for frequently accessed data
- [ ] Optimize database queries
- [ ] Add database indexes
- [ ] Compress API responses
- [ ] Implement CDN for static assets

## 🔧 Troubleshooting

### Issue: CORS Error

**Symptom:** Browser console shows CORS error

**Solution:**
1. Check backend CORS configuration
2. Ensure `Access-Control-Allow-Origin` header is set
3. Verify frontend origin is whitelisted

### Issue: 401 Unauthorized

**Symptom:** All protected API calls fail with 401

**Solution:**
1. Check if token is being sent in Authorization header
2. Verify token format: `Bearer {token}`
3. Check token expiry
4. Clear localStorage and login again

### Issue: Data Not Loading

**Symptom:** Components show loading state indefinitely

**Solution:**
1. Open browser DevTools Network tab
2. Check if API requests are being made
3. Verify API endpoint URLs are correct
4. Check response status and data format

### Issue: OTP Not Sending

**Symptom:** OTP request succeeds but user doesn't receive code

**Solution:**
1. Check backend email/SMS service configuration
2. Verify email/phone number format
3. Check spam folder for emails
4. Review backend logs for sending errors

## 📝 Development Workflow

1. **Start Backend Server**
   ```bash
   # Example for Node.js
   cd backend
   npm run dev
   ```

2. **Start Frontend**
   ```bash
   cd webapps
   npm run dev
   ```

3. **Test Authentication Flow**
   - Go to http://localhost:3000/login
   - Enter email/phone
   - Check backend logs for OTP
   - Enter OTP
   - Verify redirect to dashboard

4. **Test API Endpoints**
   - Open browser DevTools Network tab
   - Navigate through the app
   - Monitor API requests and responses
   - Check for errors in console

5. **Verify Data Persistence**
   - Add an expense
   - Refresh page
   - Verify expense still appears
   - Check database to confirm data is saved

## 🚀 Deployment Checklist

### Frontend Deployment

- [ ] Update `VITE_API_BASE_URL` to production API URL
- [ ] Build production bundle: `npm run build`
- [ ] Test production build locally: `npm run preview`
- [ ] Deploy `dist/` folder to hosting (Vercel, Netlify, etc.)
- [ ] Configure custom domain (optional)
- [ ] Enable HTTPS
- [ ] Test deployed app

### Backend Deployment

- [ ] Set environment variables for production
- [ ] Update CORS to allow production frontend URL
- [ ] Deploy to hosting (Heroku, AWS, DigitalOcean, etc.)
- [ ] Configure database connection
- [ ] Set up email/SMS service for OTPs
- [ ] Enable HTTPS
- [ ] Set up monitoring and logging
- [ ] Test all API endpoints in production

## 📚 Additional Resources

- [TypeScript Types Reference](./src/types/index.ts)
- [API Service Implementation](./src/services/api.service.ts)
- [Component Documentation](./COMPONENT_GUIDE.md)
- [Full Project Documentation](./DOCUMENTATION.md)

---

**Note:** This checklist assumes you're building a REST API backend. Adjust accordingly if using GraphQL or other API architectures.
