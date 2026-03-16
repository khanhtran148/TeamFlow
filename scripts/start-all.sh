#!/usr/bin/env bash
# TeamFlow — Start all services for local development (macOS/Linux)
# Usage: ./scripts/start-all.sh
# Stop:  ./scripts/start-all.sh stop

set -euo pipefail
cd "$(dirname "$0")/.."
ROOT=$(pwd)

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

log()  { echo -e "${CYAN}[TeamFlow]${NC} $1"; }
ok()   { echo -e "${GREEN}[✓]${NC} $1"; }
warn() { echo -e "${YELLOW}[!]${NC} $1"; }
err()  { echo -e "${RED}[✗]${NC} $1"; }

PID_DIR="$ROOT/.pids"
mkdir -p "$PID_DIR"

stop_all() {
    log "Stopping all services..."
    for pidfile in "$PID_DIR"/*.pid; do
        [ -f "$pidfile" ] || continue
        pid=$(cat "$pidfile")
        name=$(basename "$pidfile" .pid)
        if kill -0 "$pid" 2>/dev/null; then
            kill "$pid" 2>/dev/null && ok "Stopped $name (PID $pid)"
        fi
        rm -f "$pidfile"
    done
    log "All services stopped."
    exit 0
}

if [ "${1:-}" = "stop" ]; then
    stop_all
fi

# Trap to clean up on Ctrl+C
cleanup() {
    echo ""
    stop_all
}
trap cleanup INT TERM

# ─── 1. Infrastructure (Docker) ───
log "Starting infrastructure (PostgreSQL, RabbitMQ, MailHog)..."
docker compose up -d postgres rabbitmq mailhog 2>/dev/null || docker-compose up -d postgres rabbitmq mailhog
ok "Infrastructure containers started"

# Wait for PostgreSQL
log "Waiting for PostgreSQL..."
for i in $(seq 1 30); do
    if docker exec teamflow-postgres pg_isready -U teamflow -d teamflow -q 2>/dev/null; then
        ok "PostgreSQL ready"
        break
    fi
    [ "$i" -eq 30 ] && { err "PostgreSQL not ready after 60s"; exit 1; }
    sleep 2
done

# Wait for RabbitMQ
log "Waiting for RabbitMQ..."
for i in $(seq 1 30); do
    if docker exec teamflow-rabbitmq rabbitmq-diagnostics check_port_connectivity -q 2>/dev/null; then
        ok "RabbitMQ ready"
        break
    fi
    [ "$i" -eq 30 ] && { err "RabbitMQ not ready after 60s"; exit 1; }
    sleep 2
done

# ─── 2. Apply EF Core Migrations ───
log "Applying database migrations..."
dotnet ef database update \
    --project src/core/TeamFlow.Infrastructure/TeamFlow.Infrastructure.csproj \
    --startup-project src/apps/TeamFlow.Api/TeamFlow.Api.csproj \
    --no-build 2>/dev/null || \
dotnet ef database update \
    --project src/core/TeamFlow.Infrastructure/TeamFlow.Infrastructure.csproj \
    --startup-project src/apps/TeamFlow.Api/TeamFlow.Api.csproj
ok "Migrations applied"

# ─── 3. Build .NET solution ───
log "Building .NET solution..."
dotnet build TeamFlow.slnx --no-restore -q 2>/dev/null || dotnet build TeamFlow.slnx -q
ok "Solution built"

# ─── 4. Start API (background) ───
log "Starting TeamFlow API (http://localhost:5210)..."
dotnet run --project src/apps/TeamFlow.Api/TeamFlow.Api.csproj --no-build \
    > "$ROOT/logs/api.log" 2>&1 &
echo $! > "$PID_DIR/api.pid"

# Wait for API health
for i in $(seq 1 30); do
    if curl -sf http://localhost:5210/health > /dev/null 2>&1; then
        ok "API ready at http://localhost:5210"
        break
    fi
    [ "$i" -eq 30 ] && { err "API not ready after 60s"; exit 1; }
    sleep 2
done

# ─── 5. Start Background Services (background) ───
log "Starting Background Services..."
dotnet run --project src/apps/TeamFlow.BackgroundServices/TeamFlow.BackgroundServices.csproj --no-build \
    > "$ROOT/logs/background.log" 2>&1 &
echo $! > "$PID_DIR/background.pid"
ok "Background Services started"

# ─── 6. Start Frontend (background) ───
log "Starting Frontend (http://localhost:3000)..."
cd src/apps/teamflow-web
npm run dev > "$ROOT/logs/frontend.log" 2>&1 &
echo $! > "$PID_DIR/frontend.pid"
cd "$ROOT"

for i in $(seq 1 30); do
    if curl -sf http://localhost:3000 > /dev/null 2>&1; then
        ok "Frontend ready at http://localhost:3000"
        break
    fi
    [ "$i" -eq 30 ] && { warn "Frontend still starting... check logs/frontend.log"; }
    sleep 2
done

# ─── Done ───
echo ""
echo -e "${GREEN}═══════════════════════════════════════════════${NC}"
echo -e "${GREEN}  TeamFlow is running!${NC}"
echo -e "${GREEN}═══════════════════════════════════════════════${NC}"
echo ""
echo -e "  Frontend:     ${CYAN}http://localhost:3000${NC}"
echo -e "  API:          ${CYAN}http://localhost:5210${NC}"
echo -e "  RabbitMQ UI:  ${CYAN}http://localhost:15672${NC}"
echo -e "  MailHog UI:   ${CYAN}http://localhost:8025${NC}"
echo ""
echo -e "  Admin login:  admin@teamflow.dev / Admin@1234"
echo ""
echo -e "  Logs:         logs/api.log, logs/background.log, logs/frontend.log"
echo -e "  Stop:         ${YELLOW}./scripts/start-all.sh stop${NC}"
echo -e "                or press ${YELLOW}Ctrl+C${NC}"
echo ""

# Keep script alive so Ctrl+C triggers cleanup
wait
