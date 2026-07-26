#Requires -Version 5.1
# deploy-api.ps1 — Build, package, and deploy ExpensesBackend API to Azure Web App

$ErrorActionPreference = "Stop"

$BackendDir    = Join-Path $PSScriptRoot "expensesBackend"
$PublishDir    = Join-Path $BackendDir "publish-linux"
$ZipPath       = Join-Path $BackendDir "backend-linux.zip"
$ResourceGroup = "expense-tracker-rg"
$AppName       = "exptr-backend-api"

function Write-Step($msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }
function Write-Ok($msg)   { Write-Host "    $msg" -ForegroundColor Green }
function Fail($msg)       { Write-Host "`n[FAILED] $msg" -ForegroundColor Red; exit 1 }

# ── Pre-flight: Azure CLI login check ────────────────────────────────────────
Write-Step "Checking Azure CLI login"
$account = az account show 2>&1
if ($LASTEXITCODE -ne 0) { Fail "Not logged in to Azure CLI. Run: az login" }
Write-Ok "Logged in as: $((($account | ConvertFrom-Json).user.name))"

# ── Step 1: Publish ───────────────────────────────────────────────────────────
Write-Step "Publishing API (Release / linux-x64)"
Push-Location $BackendDir
dotnet publish ExpensesBackend.API.csproj -c Release -r linux-x64 --self-contained false -o $PublishDir
if ($LASTEXITCODE -ne 0) { Pop-Location; Fail "dotnet publish failed" }
Pop-Location
Write-Ok "Published to $PublishDir"

# ── Step 2: Package ───────────────────────────────────────────────────────────
Write-Step "Creating deployment zip"
if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::Open($ZipPath, 'Create')
Get-ChildItem -Path $PublishDir -Recurse -File | ForEach-Object {
    $entryName = $_.FullName.Substring($PublishDir.Length + 1).Replace('\', '/')
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $entryName) | Out-Null
}
$zip.Dispose()
$zipSizeMB = [math]::Round((Get-Item $ZipPath).Length / 1MB, 1)
Write-Ok "Zip created: $ZipPath ($zipSizeMB MB)"

# ── Step 3: Deploy ────────────────────────────────────────────────────────────
Write-Step "Deploying zip to Azure Web App '$AppName'"
$ErrorActionPreference = "Continue"   # az writes WARNING to stderr; don't treat as fatal
$deployOutput = az webapp deploy `
    --resource-group $ResourceGroup `
    --name $AppName `
    --src-path $ZipPath `
    --type zip `
    --async false `
    --only-show-errors 2>&1
$deployExit = $LASTEXITCODE
$ErrorActionPreference = "Stop"

if ($deployExit -ne 0) {
    Write-Host $deployOutput -ForegroundColor Yellow
    Fail "az webapp deploy failed (exit $deployExit)"
}
Write-Ok "Deploy complete"

# ── Step 4: Restart ───────────────────────────────────────────────────────────
Write-Step "Restarting web app"
$ErrorActionPreference = "Continue"
az webapp restart --name $AppName --resource-group $ResourceGroup --only-show-errors | Out-Null
$restartExit = $LASTEXITCODE
$ErrorActionPreference = "Stop"
if ($restartExit -ne 0) { Fail "az webapp restart failed" }
Write-Ok "App restarted"

Write-Host "`n[DONE] API deployed -> https://$AppName.azurewebsites.net`n" -ForegroundColor Green
