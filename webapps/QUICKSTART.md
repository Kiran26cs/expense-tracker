# ⚡ Quick Start Guide

Get up and running in 5 minutes!

## 1️⃣ Install Dependencies (2 minutes)

```bash
cd d:\flutterRepo\webapps
npm install
```

This installs:
- React 18
- TypeScript
- Vite
- Recharts
- React Router
- date-fns

## 2️⃣ Configure Environment (30 seconds)

Create a `.env` file:

```bash
# Copy the example file
copy .env.example .env

# Or create manually with:
VITE_API_BASE_URL=http://localhost:8000/api
```

> **Note**: Update the API URL when you have a backend ready

## 3️⃣ Start Development Server (30 seconds)

```bash
npm run dev
```

You should see:
```
  VITE v5.0.8  ready in 500 ms

  ➜  Local:   http://localhost:3000/
  ➜  Network: use --host to expose
```

## 4️⃣ Open in Browser (10 seconds)

Visit: **http://localhost:3000**

You'll see the login page! 🎉

## 5️⃣ Explore the App (2 minutes)

### Without Backend (Visual Exploration)
- Browse all pages via URL:
  - `/login` - Login page
  - `/signup` - Signup page
  - After logging in (needs backend):
    - `/` - Dashboard
    - `/expenses` - Expense list
    - `/expenses/add` - Add expense
    - `/budget` - Budget planner
    - `/insights` - Insights & forecast
    - `/settings` - Settings

### With Backend
1. Enter email/phone on login
2. Receive OTP (check backend logs)
3. Enter OTP to login
4. Explore full functionality!

## 🎨 Try These Features

### Theme Toggle
1. Login (or temporarily disable auth check)
2. Click moon/sun icon in top right
3. Watch the app switch between light/dark mode

### Responsive Design
1. Open browser DevTools (F12)
2. Toggle device toolbar (Ctrl+Shift+M)
3. Switch between mobile/tablet/desktop
4. Notice sidebar becomes bottom navigation on mobile

### Component Examples

**Play with the Button component:**

Create a test page `src/pages/Test.tsx`:

```tsx
import { Button } from '@/components/Button/Button';

export const TestPage = () => {
  return (
    <div style={{ padding: '2rem', display: 'flex', flexDirection: 'column', gap: '1rem' }}>
      <Button variant="primary">Primary Button</Button>
      <Button variant="secondary">Secondary Button</Button>
      <Button variant="success">Success Button</Button>
      <Button variant="warning">Warning Button</Button>
      <Button variant="danger">Danger Button</Button>
      <Button variant="ghost">Ghost Button</Button>
      <Button loading>Loading Button</Button>
      <Button fullWidth>Full Width Button</Button>
    </div>
  );
};
```

## 📁 Key Files to Explore

### 1. Main App Entry
```
src/main.tsx          # Application entry point
src/App.tsx           # Routing and auth setup
```

### 2. Example Page
```
src/pages/Dashboard/DashboardPage.tsx
```
**What to learn**: 
- Using `useApi` hook for data fetching
- Recharts integration
- Responsive grid layouts
- Loading/Error states

### 3. Reusable Components
```
src/components/Button/Button.tsx
src/components/Card/Card.tsx
src/components/Input/Input.tsx
```
**What to learn**:
- Component prop patterns
- CSS Modules usage
- TypeScript interfaces

### 4. Custom Hooks
```
src/hooks/useAuth.tsx
src/hooks/useTheme.ts
src/hooks/useApi.ts
```
**What to learn**:
- React Context patterns
- Custom hook creation
- State management

### 5. API Integration
```
src/services/api.service.ts
src/services/expense.api.ts
```
**What to learn**:
- Service class pattern
- API request handling
- TypeScript generics

## 🐛 Troubleshooting

### Issue: npm install fails

**Solution**:
```bash
# Clear npm cache
npm cache clean --force

# Try again
npm install
```

### Issue: Port 3000 already in use

**Solution**:
```bash
# Use a different port
# Edit vite.config.ts and change:
server: { port: 3001 }
```

### Issue: Module not found errors

**Solution**:
```bash
# Restart dev server
# Press Ctrl+C to stop
npm run dev
```

### Issue: TypeScript errors

**Solution**:
```bash
# Check TypeScript
npx tsc --noEmit

# If it's a path alias issue, restart VS Code
```

## 🎯 Next Steps

### Option A: Explore Frontend Only

1. **Review Components**
   - Open `src/components/` folder
   - Read component source code
   - Try modifying styles

2. **Customize Design**
   - Edit `src/styles/global.css`
   - Change CSS variables
   - See changes live!

3. **Add New Features**
   - Create new components
   - Add new pages
   - Experiment with layouts

### Option B: Connect Backend

1. **Follow Backend Guide**
   - Read `BACKEND_INTEGRATION.md`
   - Build required API endpoints
   - Test with Postman/curl

2. **Update Configuration**
   - Set correct `VITE_API_BASE_URL`
   - Ensure CORS is configured
   - Test API connectivity

3. **Test Full Flow**
   - Try login/signup
   - Add expenses
   - View dashboard
   - Test all features

## 📚 Learn More

### Documentation Files

| File | Purpose |
|------|---------|
| **PROJECT_OVERVIEW.md** | High-level project summary |
| **DOCUMENTATION.md** | Complete technical docs |
| **COMPONENT_GUIDE.md** | Component API reference |
| **BACKEND_INTEGRATION.md** | API integration checklist |
| **README.md** | Project readme |

### VS Code Tips

**Recommended Extensions**:
- ES7+ React/Redux/React-Native snippets
- TypeScript React code snippets
- CSS Modules
- Prettier - Code formatter

**Useful Shortcuts**:
- `Ctrl + P` - Quick file open
- `Ctrl + Shift + F` - Search across files
- `F12` - Go to definition
- `Alt + ←/→` - Navigate back/forward

## 🎓 Learning Path

### Day 1: Setup & Exploration
- ✅ Install and run the app
- ✅ Browse all pages
- ✅ Try theme toggle
- ✅ Test responsive design

### Day 2: Component Deep Dive
- ✅ Read component source code
- ✅ Understand prop patterns
- ✅ Try modifying components

### Day 3: State Management
- ✅ Study custom hooks
- ✅ Understand React Context
- ✅ Learn API integration patterns

### Day 4: Backend Integration
- ✅ Follow BACKEND_INTEGRATION.md
- ✅ Build API endpoints
- ✅ Test full application

### Day 5: Customization
- ✅ Customize design
- ✅ Add new features
- ✅ Prepare for deployment

## 🚀 Build for Production

When you're ready to deploy:

```bash
# Build production bundle
npm run build

# Preview production build locally
npm run preview

# Deploy dist/ folder to:
# - Vercel
# - Netlify
# - GitHub Pages
# - Your own server
```

## ✨ Pro Tips

1. **Hot Module Replacement**: Save any file and see instant updates (no refresh needed!)

2. **TypeScript Errors**: Hover over red squiggles in VS Code to see detailed error messages

3. **Console Logging**: Check browser console for:
   - API requests (Network tab)
   - React errors
   - Custom log messages

4. **Component Inspector**: Right-click any element → Inspect to see HTML structure and CSS

5. **Responsive Testing**: Use Chrome DevTools device emulator to test mobile layouts

## 💡 Common Tasks

### Change Primary Color

Edit `src/styles/global.css`:

```css
:root {
  --color-primary: #your-color-here;
}
```

### Add a New Page

1. Create file: `src/pages/YourPage/YourPage.tsx`
2. Add route in `src/App.tsx`
3. Add navigation in `src/components/Sidebar/Sidebar.tsx`

### Add a New API Endpoint

1. Add to appropriate service file in `src/services/`
2. Use `useApi` or `useMutation` hook to call it
3. Handle loading/error/success states

## 🎊 You're Ready!

You now have a **fully functional expense tracker frontend** running locally.

**Happy coding! 🚀**

---

**Need help?** Check the other documentation files or review the code examples.
