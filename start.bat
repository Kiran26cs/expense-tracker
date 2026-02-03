@echo off
echo ========================================
echo  Expense Tracker - Starting Services
echo ========================================
echo.

echo [1/3] Checking MongoDB...
tasklist /FI "IMAGENAME eq mongod.exe" 2>NUL | find /I /N "mongod.exe">NUL
if "%ERRORLEVEL%"=="0" (
    echo ✓ MongoDB is running
) else (
    echo ⚠ MongoDB is not running!
    echo   Please start MongoDB first: mongod --dbpath C:\data\db
    echo.
    pause
)

echo.
echo [2/3] Starting Backend API...
start "Expense Tracker - Backend" cmd /k "cd /d expensesBackend && echo Starting Backend API... && dotnet run"
timeout /t 3 /nobreak >nul

echo [3/3] Starting Frontend...
start "Expense Tracker - Frontend" cmd /k "cd /d webapps && echo Starting Frontend... && npm run dev"
timeout /t 5 /nobreak >nul

echo.
echo ========================================
echo  Services Started!
echo ========================================
echo.
echo Backend API:  http://localhost:5196
echo Swagger:      http://localhost:5196/swagger
echo Frontend:     http://localhost:3000
echo.
echo Opening application in browser...
timeout /t 3 /nobreak >nul
start http://localhost:3000

echo.
echo ✓ Application started successfully!
echo.
echo To stop the servers, close the terminal windows.
echo.
pause
