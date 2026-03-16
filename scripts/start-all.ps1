# TeamFlow — Start all services for local development (Windows PowerShell)
# Usage: .\scripts\start-all.ps1
# Stop:  .\scripts\start-all.ps1 -Stop

param([switch]$Stop)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

$PidDir = Join-Path $Root ".pids"
$LogDir = Join-Path $Root "logs"
New-Item -ItemType Directory -Force -Path $PidDir | Out-Null
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null

function Write-Status($msg) { Write-Host "[TeamFlow] $msg" -ForegroundColor Cyan }
function Write-Ok($msg)     { Write-Host "[OK] $msg" -ForegroundColor Green }
function Write-Warn($msg)   { Write-Host "[!] $msg" -ForegroundColor Yellow }
function Write-Err($msg)    { Write-Host "[X] $msg" -ForegroundColor Red }

function Stop-AllServices {
    Write-Status "Stopping all services..."
    Get-ChildItem "$PidDir\*.pid" -ErrorAction SilentlyContinue | ForEach-Object {
        $pid = Get-Content $_.FullName
        $name = $_.BaseName
        try {
            $proc = Get-Process -Id $pid -ErrorAction SilentlyContinue
            if ($proc) {
                Stop-Process -Id $pid -Force
                Write-Ok "Stopped $name (PID $pid)"
            }
        } catch {}
        Remove-Item $_.FullName -Force
    }
    Write-Status "All services stopped."
}

if ($Stop) {
    Stop-AllServices
    exit 0
}

# --- 1. Infrastructure (Docker) ---
Write-Status "Starting infrastructure (PostgreSQL, RabbitMQ, MailHog)..."
docker compose up -d postgres rabbitmq mailhog
Write-Ok "Infrastructure containers started"

# Wait for PostgreSQL
Write-Status "Waiting for PostgreSQL..."
for ($i = 1; $i -le 30; $i++) {
    $ready = docker exec teamflow-postgres pg_isready -U teamflow -d teamflow 2>$null
    if ($LASTEXITCODE -eq 0) { Write-Ok "PostgreSQL ready"; break }
    if ($i -eq 30) { Write-Err "PostgreSQL not ready after 60s"; exit 1 }
    Start-Sleep 2
}

# Wait for RabbitMQ
Write-Status "Waiting for RabbitMQ..."
for ($i = 1; $i -le 30; $i++) {
    docker exec teamflow-rabbitmq rabbitmq-diagnostics check_port_connectivity -q 2>$null
    if ($LASTEXITCODE -eq 0) { Write-Ok "RabbitMQ ready"; break }
    if ($i -eq 30) { Write-Err "RabbitMQ not ready after 60s"; exit 1 }
    Start-Sleep 2
}

# --- 2. Apply EF Core Migrations ---
Write-Status "Applying database migrations..."
dotnet ef database update `
    --project src/core/TeamFlow.Infrastructure/TeamFlow.Infrastructure.csproj `
    --startup-project src/apps/TeamFlow.Api/TeamFlow.Api.csproj
Write-Ok "Migrations applied"

# --- 3. Build .NET solution ---
Write-Status "Building .NET solution..."
dotnet build TeamFlow.slnx -q
Write-Ok "Solution built"

# --- 4. Start API (background) ---
Write-Status "Starting TeamFlow API (http://localhost:5210)..."
$apiProc = Start-Process -FilePath "dotnet" `
    -ArgumentList "run","--project","src/apps/TeamFlow.Api/TeamFlow.Api.csproj","--no-build" `
    -RedirectStandardOutput "$LogDir\api.log" `
    -RedirectStandardError "$LogDir\api-error.log" `
    -PassThru -WindowStyle Hidden
$apiProc.Id | Out-File "$PidDir\api.pid"

# Wait for API health
for ($i = 1; $i -le 30; $i++) {
    try {
        $resp = Invoke-WebRequest -Uri "http://localhost:5210/health" -UseBasicParsing -TimeoutSec 2
        if ($resp.StatusCode -eq 200) { Write-Ok "API ready at http://localhost:5210"; break }
    } catch {}
    if ($i -eq 30) { Write-Err "API not ready after 60s"; exit 1 }
    Start-Sleep 2
}

# --- 5. Start Background Services (background) ---
Write-Status "Starting Background Services..."
$bgProc = Start-Process -FilePath "dotnet" `
    -ArgumentList "run","--project","src/apps/TeamFlow.BackgroundServices/TeamFlow.BackgroundServices.csproj","--no-build" `
    -RedirectStandardOutput "$LogDir\background.log" `
    -RedirectStandardError "$LogDir\background-error.log" `
    -PassThru -WindowStyle Hidden
$bgProc.Id | Out-File "$PidDir\background.pid"
Write-Ok "Background Services started"

# --- 6. Start Frontend (background) ---
Write-Status "Starting Frontend (http://localhost:3000)..."
$feProc = Start-Process -FilePath "npm" `
    -ArgumentList "run","dev" `
    -WorkingDirectory "src/apps/teamflow-web" `
    -RedirectStandardOutput "$LogDir\frontend.log" `
    -RedirectStandardError "$LogDir\frontend-error.log" `
    -PassThru -WindowStyle Hidden
$feProc.Id | Out-File "$PidDir\frontend.pid"

for ($i = 1; $i -le 30; $i++) {
    try {
        $resp = Invoke-WebRequest -Uri "http://localhost:3000" -UseBasicParsing -TimeoutSec 2
        if ($resp.StatusCode -eq 200) { Write-Ok "Frontend ready at http://localhost:3000"; break }
    } catch {}
    if ($i -eq 30) { Write-Warn "Frontend still starting... check logs\frontend.log" }
    Start-Sleep 2
}

# --- Done ---
Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "  TeamFlow is running!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Frontend:     http://localhost:3000" -ForegroundColor Cyan
Write-Host "  API:          http://localhost:5210" -ForegroundColor Cyan
Write-Host "  RabbitMQ UI:  http://localhost:15672" -ForegroundColor Cyan
Write-Host "  MailHog UI:   http://localhost:8025" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Admin login:  admin@teamflow.dev / Admin@1234"
Write-Host ""
Write-Host "  Logs:         logs\api.log, logs\background.log, logs\frontend.log"
Write-Host "  Stop:         .\scripts\start-all.ps1 -Stop" -ForegroundColor Yellow
Write-Host ""
