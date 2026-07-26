#Requires -Version 5.1
# deploy-admin.ps1 — Build and deploy Angular admin app to Azure Static Web Apps
#
# Azure resource : expense-tracker-admin
# Default URL    : https://brave-beach-0c5fe3b00.7.azurestaticapps.net
# Custom domain  : https://admin.nidhiwise.com  (configure in Cloudflare — see README)
#
# Prerequisites:
#   1. Install SWA CLI: npm install -g @azure/static-web-apps-cli

$ErrorActionPreference = "Stop"

$AppDir    = Join-Path $PSScriptRoot "expensesAdminApp"
$OutputDir = Join-Path $AppDir "dist\expenses-admin-app\browser"

function Write-Step($msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }
function Write-Ok($msg)   { Write-Host "    $msg" -ForegroundColor Green }
function Fail($msg)       { Write-Host "`n[FAILED] $msg" -ForegroundColor Red; exit 1 }

# ── Guard: deployment token ───────────────────────────────────────────────────
if (-not $env:SWA_CLI_DEPLOYMENT_TOKEN) {
    Write-Step "Fetching deployment token via Azure CLI"
    $token = az staticwebapp secrets list `
        --name "expense-tracker-admin" `
        --resource-group "expense-tracker-rg" `
        --query "properties.apiKey" -o tsv 2>$null
    if ($token) {
        $env:SWA_CLI_DEPLOYMENT_TOKEN = $token
        Write-Ok "Token retrieved from Azure CLI"
    } else {
        Write-Host @"

[ERROR] SWA_CLI_DEPLOYMENT_TOKEN is not set and Azure CLI fetch failed.

Either:
  1. Run: az login  (then re-run this script)
  2. Or set manually:
       `$env:SWA_CLI_DEPLOYMENT_TOKEN = "<token from Azure Portal>"

"@ -ForegroundColor Red
        exit 1
    }
}

# ── Step 1: Install dependencies ─────────────────────────────────────────────
Write-Step "Installing npm dependencies"
Push-Location $AppDir
npm install --legacy-peer-deps
if ($LASTEXITCODE -ne 0) { Pop-Location; Fail "npm install failed" }
Write-Ok "Dependencies installed"

# ── Step 2: Build ─────────────────────────────────────────────────────────────
Write-Step "Building Angular admin app (production)"
npx ng build --configuration=production
if ($LASTEXITCODE -ne 0) { Pop-Location; Fail "ng build failed" }
Pop-Location
Write-Ok "Build output: $OutputDir"

# ── Step 3: Deploy ────────────────────────────────────────────────────────────
Write-Step "Deploying to Azure Static Web Apps (expense-tracker-admin)"
swa deploy $OutputDir --env production
if ($LASTEXITCODE -ne 0) { Fail "swa deploy failed" }

Write-Host "`n[DONE] Admin app deployed" -ForegroundColor Green
Write-Host "       Azure URL : https://brave-beach-0c5fe3b00.7.azurestaticapps.net" -ForegroundColor Green
Write-Host "       Custom URL: https://admin.nidhiwise.com  (after Cloudflare DNS)" -ForegroundColor Green
