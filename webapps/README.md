# ExpenseTracker - Mobile-First Expense Tracker

A modern, mobile-first expense tracking web application built with React and TypeScript.

## Features

- 📱 Mobile-first responsive design
- 🌓 Light & dark mode support
- 📊 Interactive charts and visualizations
- 💰 Budget planning and tracking
- 🔮 Cash flow forecasting
- 🎯 "Can I Buy This?" simulator
- 📥 CSV import functionality
- 🔔 Recurring expense alerts

## Tech Stack

- React 18 with TypeScript
- Vite for fast development
- Recharts for data visualization
- React Router for navigation
- CSS Variables for theming
- Date-fns for date manipulation

## Getting Started

1. Install dependencies:
```bash
npm install
```

2. Start development server:
```bash
npm run dev
```

3. Build for production:
```bash
npm run build
```

## Project Structure

```
src/
├── components/       # Reusable UI components
├── pages/           # Page-level components
├── layouts/         # Layout components
├── hooks/           # Custom React hooks
├── services/        # API services
├── types/           # TypeScript type definitions
├── utils/           # Utility functions
└── styles/          # Global styles and CSS variables
```

## API Integration

This frontend expects a REST API backend with the following endpoints:

- `POST /api/auth/login` - Login with email/phone + OTP
- `POST /api/auth/signup` - User registration
- `GET /api/dashboard` - Dashboard summary data
- `GET /api/expenses` - List expenses
- `POST /api/expenses` - Create expense
- `GET /api/budgets` - Budget data
- `GET /api/forecast` - Cash flow forecast
- `POST /api/simulator` - Purchase impact simulation
- `POST /api/import` - CSV import

## Design System

The app follows a sleek fintech design with:
- Rounded cards with soft shadows
- Calm color gradients
- Modern sans-serif typography
- Smooth 150-250ms transitions
- Clear visual indicators (green/amber/red)
- Friendly, reassuring tone

## License

Proprietary
