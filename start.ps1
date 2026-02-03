# Expense Tracker - Startup Script
# This script starts both backend and frontend services

Write-Host "🚀 Starting Expense Tracker Application..." -ForegroundColor Cyan
Write-Host ""

# Check if MongoDB is running
Write-Host "📊 Checking MongoDB..." -ForegroundColor Yellow
$mongoProcess = Get-Process mongod -ErrorAction SilentlyContinue
if ($null -eq $mongoProcess) {
    Write-Host "⚠️  MongoDB is not running. Please start MongoDB first." -ForegroundColor Red
    Write-Host "   Run: mongod --dbpath C:\data\db" -ForegroundColor Yellow
    Write-Host ""
    $startMongo = Read-Host "Do you want to continue anyway? (y/n)"
    if ($startMongo -ne "y") {
        exit
    }
} else {
    Write-Host "✅ MongoDB is running" -ForegroundColor Green
}

Write-Host ""

# Start Backend
Write-Host "🔧 Starting Backend API..." -ForegroundColor Yellow
$backendPath = Join-Path $PSScriptRoot "expensesBackend"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$backendPath'; Write-Host '🔧 Backend API Starting...' -ForegroundColor Cyan; dotnet run"

Write-Host "✅ Backend starting in new window..." -ForegroundColor Green
Start-Sleep -Seconds 3

# Start Frontend
Write-Host "🎨 Starting Frontend..." -ForegroundColor Yellow
$frontendPath = Join-Path $PSScriptRoot "webapps"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$frontendPath'; Write-Host '🎨 Frontend Starting...' -ForegroundColor Cyan; npm run dev"

Write-Host "✅ Frontend starting in new window..." -ForegroundColor Green
Write-Host ""
Write-Host "⏳ Waiting for services to start..." -ForegroundColor Cyan
Start-Sleep -Seconds 5

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✨ Expense Tracker Started!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📍 Backend API:  http://localhost:5196" -ForegroundColor Yellow
Write-Host "📍 Swagger UI:   http://localhost:5196/swagger" -ForegroundColor Yellow
Write-Host "📍 Frontend:     http://localhost:3000" -ForegroundColor Yellow
Write-Host ""
Write-Host "Press any key to open the application in browser..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

Start-Process "http://localhost:3000"

Write-Host ""
Write-Host "✅ Application opened in browser!" -ForegroundColor Green
Write-Host "🛑 To stop the servers, close the terminal windows." -ForegroundColor Yellow
Write-Host ""
