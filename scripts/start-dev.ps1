param(
    [switch]$Install,
    [switch]$Migrate,
    [switch]$Seed,
    [switch]$OpenBrowser
)

$ErrorActionPreference = "Stop"

function Assert-Command($Name, $InstallHint) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Missing required command '$Name'. $InstallHint"
    }
}

function Quote-ForPowerShell($Value) {
    return "'" + ($Value -replace "'", "''") + "'"
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$backendProject = Join-Path $repoRoot "backend\src\Bcmp.Api\Bcmp.Api.csproj"
$infrastructureProject = Join-Path $repoRoot "backend\src\Bcmp.Infrastructure\Bcmp.Infrastructure.csproj"
$frontendDir = Join-Path $repoRoot "frontend"
$frontendEnv = Join-Path $frontendDir ".env"
$frontendEnvExample = Join-Path $frontendDir ".env.example"

Assert-Command "dotnet" "Install the .NET SDK, then try again."
Assert-Command "npm" "Install Node.js, then try again."

if (-not (Test-Path $frontendEnv) -and (Test-Path $frontendEnvExample)) {
    Copy-Item -Path $frontendEnvExample -Destination $frontendEnv
    Write-Host "Created frontend\.env from frontend\.env.example. Check VITE_GOOGLE_CLIENT_ID before signing in." -ForegroundColor Yellow
}

if ($Install) {
    Write-Host "Restoring backend packages..." -ForegroundColor Cyan
    dotnet restore (Join-Path $repoRoot "backend\BodyCorporatePortal.slnx")

    Write-Host "Installing frontend packages..." -ForegroundColor Cyan
    npm --prefix $frontendDir install
}

if ($Migrate) {
    Write-Host "Applying EF Core migrations..." -ForegroundColor Cyan
    dotnet ef database update --project $infrastructureProject --startup-project $backendProject
}

if ($Seed) {
    Write-Host "Seeding bootstrap portal admin..." -ForegroundColor Cyan
    dotnet run --project $backendProject -- --seed
}

$apiCommand = "Set-Location " + (Quote-ForPowerShell $repoRoot) + "; dotnet run --project " + (Quote-ForPowerShell $backendProject)
$frontendCommand = "Set-Location " + (Quote-ForPowerShell $repoRoot) + "; npm run dev"

Write-Host "Starting backend API on http://localhost:5151..." -ForegroundColor Green
Start-Process powershell -ArgumentList @("-NoExit", "-ExecutionPolicy", "Bypass", "-Command", $apiCommand)

Write-Host "Starting frontend on http://localhost:5173..." -ForegroundColor Green
Start-Process powershell -ArgumentList @("-NoExit", "-ExecutionPolicy", "Bypass", "-Command", $frontendCommand)

if ($OpenBrowser) {
    Start-Sleep -Seconds 3
    Start-Process "http://localhost:5173"
}

Write-Host ""
Write-Host "Dev app is starting. Use the two new PowerShell windows for logs; close them to stop the app." -ForegroundColor Green
