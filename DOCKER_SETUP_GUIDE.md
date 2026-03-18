# GTEK FSM - Docker Setup Guide

## Overview

Docker provides consistent development, staging, and production environments without OS-specific setup. This guide covers Docker setup for GTEK FSM local development.

---

## Prerequisites

### 1. Install Docker Desktop

**macOS (Intel & Apple Silicon):**
```bash
# Download from https://www.docker.com/products/docker-desktop
# Or use Homebrew:
brew install docker --cask
```

**Ubuntu/Debian (Linux):**
```bash
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Add current user to docker group
sudo usermod -aG docker $USER
newgrp docker
```

**Windows 11:**
- Download [Docker Desktop for Windows](https://www.docker.com/products/docker-desktop)
- Enable WSL 2 backend (recommended for performance)
- Restart computer

**Verify Installation:**
```bash
docker --version        # Docker version 20.10+
docker-compose --version  # Docker Compose 2.0+
docker run hello-world  # Should print success message
```

### 2. Resource Configuration

**Recommended (for full stack development):**

Edit Docker Desktop Settings:

| Setting | Recommended | Minimum |
|---------|-------------|---------|
| CPU | 4 cores | 2 cores |
| Memory | 8 GB | 4 GB |
| Disk | 50 GB | 20 GB |
| Swap | 2 GB | 1 GB |

**Check Current:**
```bash
docker system df
docker stats  # Real-time resource usage
```

---

## Docker Compose Setup

### 1. Environment File

Copy template:
```bash
cp .env.example .env
```

Edit for Docker:
```bash
# .env
ASPNETCORE_ENVIRONMENT=Development
SA_PASSWORD=YourStrong!Passw0rd  # Min 8 chars, special char required
SQL_SERVER_HOST=sqlserver        # Service name in compose
SQL_SERVER_PORT=1433
SQL_SERVER_DATABASE=GTEK_FSM_Local
API_PORT=5000
```

### 2. Review docker-compose.yml

Located at: `docker-compose.yml` in project root

**Services:**
```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2019-latest
    port: 1433
    volumes:
      - sqlserver_data:/var/opt/mssql/data
    environment:
      SA_PASSWORD: ${SA_PASSWORD}

  api:
    build: ./backend/api
    port: 5000
    depends_on:
      - sqlserver
    environment:
      Database__ConnectionString: "Server=sqlserver;..."

  portal:
    build: ./web-portal
    port: 5001
    depends_on:
      - api
```

### 3. Build Images

**Option A: Automatic (Recommended)**

```bash
docker-compose up --build
```

This automatically:
1. Builds Docker images from `Dockerfile`s
2. Pulls base images (SQL Server, .NET runtime)
3. Creates containers
4. Starts services
5. Shows logs

**Option B: Manual**

```bash
# Build each service individually
docker-compose build sqlserver
docker-compose build api
docker-compose build portal

# Then start
docker-compose up
```

### 4. View Services Running

```bash
# List running containers
docker-compose ps

# Expected output:
# NAME              STATUS      PORTS
# sqlserver         Up 2m       1433/tcp
# api               Up 1m       5000/tcp
# portal            Up 30s      5001/tcp
```

---

## Common Docker Commands

### Starting & Stopping

```bash
# Start all services (foreground - see logs)
docker-compose up

# Start in background (daemon mode)
docker-compose up -d

# Stop all services
docker-compose down

# Stop and remove volumes (fresh start)
docker-compose down -v

# Restart specific service
docker-compose restart api
```

### Viewing Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f api

# Last 100 lines, follow new output
docker-compose logs -f --tail=100

# Timestamps
docker-compose logs -t api
```

### Database Management

```bash
# Connect to SQL Server in container
docker-compose exec sqlserver sqlcmd -S . -U sa -P $SA_PASSWORD

# Check databases
> SELECT name FROM sys.databases;

# Check GTEK_FSM_Local
> USE GTEK_FSM_Local;
> GO
> SELECT COUNT(*) FROM sys.tables;
> GO
> exit

# Or use SQL tools client
# Server: localhost:1433
# User: sa
# Password: {SA_PASSWORD from .env}
```

### Rebuilding After Changes

```bash
# C# code changed - rebuild affected service
docker-compose build api

# Then restart
docker-compose restart api

# Or in one command (stop old, remove image, rebuild, restart)
docker-compose up -d --build api

# Complete rebuild (all services)
docker-compose up --build --force-recreate
```

---

## Volume Management

### Data Persistence

**SQL Server Data:**
```yaml
volumes:
  sqlserver_data:
    driver: local
```

- **Location:** Docker-managed directory (not on host filesystem)
- **Persists:** When container stops/starts
- **Cleared:** Only with `docker-compose down -v`

**Storage (Files):**
```yaml
volumes:
  - ./storage/docker:/app/storage
```

- **Location:** `./storage/docker` on host machine
- **Visibility:** Accessible from host
- **Persists:** Even after `docker-compose down`

### View Volumes

```bash
# List all volumes
docker volume ls

# Inspect specific volume
docker volume inspect gtek-fsm_sqlserver_data

# Remove unused volumes
docker volume prune
```

### Backup Database

```bash
# Create backup file
docker-compose exec sqlserver \
  sqlcmd -S . -U sa -P $SA_PASSWORD \
  -Q "BACKUP DATABASE GTEK_FSM_Local TO DISK='/var/opt/mssql/backup/backup.bak'"

# Copy to host
docker cp $(docker-compose ps -q sqlserver):/var/opt/mssql/backup/backup.bak ./backup.bak

# Or volume mount approach (add to docker-compose.yml):
# volumes:
#   - ./backups:/var/opt/mssql/backup
```

---

## Network Configuration

### Container Networking

**Automatic bridge network created:**
```
gtek-fsm_default

Containers connected:
  - sqlserver (hostname: sqlserver)
  - api (hostname: api)
  - portal (hostname: portal)
```

**Service Discovery:**
- API talks to SQL Server: `Server=sqlserver;...`
- Portal talks to API: `http://api:5000`

### Port Mapping

| Service | Container Port | Host Port | Access |
|---------|---------------|-----------| -------|
| API | 5000 | 5000 | http://localhost:5000 |
| Portal | 5001 | 5001 | http://localhost:5001 |
| SQL Server | 1433 | 1433 | localhost:1433 |

### Custom Network (Advanced)

```yaml
networks:
  core:
    driver: bridge

services:
  api:
    networks:
      - core
  database:
    networks:
      - core
```

---

## Troubleshooting

### Issue: Container won't start

```bash
# Check error message
docker-compose logs api

# Common causes:
# 1. Port already in use
lsof -i :5000
kill -9 <PID>

# 2. Invalid environment variable
grep SA_PASSWORD .env

# 3. Image build failed
docker-compose build --no-cache api
```

### Issue: Can't connect to SQL Server

```bash
# Verify SQL Server is running
docker-compose ps sqlserver

# Check connectivity from host
sqlcmd -S localhost,1433 -U sa -P $SA_PASSWORD

# Check from API container
docker-compose exec api sqlcmd -S sqlserver -U sa -P $SA_PASSWORD

# Verify connection string in .env
# Should be: Server=sqlserver;... (not localhost from inside container)
```

### Issue: Out of disk space

```bash
# Check space
docker system df

# Clean up unused resources
docker system prune -a

# Remove dangling volumes
docker volume prune

# Remove build cache
docker builder prune

# Increase Docker desktop disk allocation (Settings)
```

### Issue: Performance is slow

```bash
# Check resource usage
docker stats

# Increase Docker desktop resources:
# Settings → Resources → Increase CPU/Memory

# For Linux: Edit /etc/docker/daemon.json
{
  "storage-driver": "overlay2"
}
```

### Issue: Permission denied

```bash
# Linux: Add to docker group
sudo usermod -aG docker $USER
newgrp docker

# Or use sudo (temporary)
sudo docker-compose up
```

### Issue: Migrations not applied

```bash
# Check migration logic in Dockerfile/entrypoint
docker-compose logs api

# Manually apply (inside container)
docker-compose exec api \
  dotnet GTEK.FSM.Backend.Api.dll --migrate

# Or reset database
docker-compose down -v
docker-compose up --build
```

---

## Docker Compose Workflows

### Daily Development

```bash
# Morning: Start services
docker-compose up -d

# Work normally
# - Code in VS Code
# - Services auto-reload (if configured)
# - Check logs anytime: docker-compose logs -f api

# Rebuild when C# code changes
docker-compose build api
docker-compose restart api

# Evening: Stop services
docker-compose stop
```

### Full Reset (Fresh Start)

```bash
# Complete cleanup
docker-compose down -v

# Full rebuild
docker-compose up --build

# Verify services
docker-compose ps
docker-compose logs api
```

### Multi-Environment Setup

```bash
# Development environment
docker-compose -f docker-compose.yml up

# Staging environment (separate file)
docker-compose -f docker-compose.staging.yml up

# Or use environment files
docker-compose --env-file .env.dev up
docker-compose --env-file .env.staging up
```

---

## Production Considerations (Phase 11)

### Not Recommended for Production
- **Local volumes** → Use cloud storage (Blob/S3)
- **Docker Compose** → Use Kubernetes/AKS
- **Self-managed SQL** → Use Managed SQL Database

### Production Setup (Future)
```bash
# Use registry
docker login myregistry.azurecr.io
docker-compose push

# Deploy to Azure Container Instances
az container create --image myregistry.azurecr.io/api:latest

# Or Kubernetes
kubectl apply -f k8s/
```

---

## Docker Best Practices

1. **Keep `.dockerignore` updated** - excludes node_modules, .git, etc.
2. **Use .env files** - never hardcode secrets
3. **Multi-stage builds** - reduce image size
4. **Health checks** - automatic container restart on failure
5. **Logging** - centralize logs for debugging
6. **Security** - scan images for vulnerabilities

```bash
# Scan image for vulnerabilities
docker scan gtek-fsm:latest

# View image layers and size
docker history gtek-fsm --no-trunc
```

---

## References

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Docs](https://docs.docker.com/compose/)
- [SQL Server on Docker](https://hub.docker.com/_/microsoft-mssql-server)
- [.NET Docker Images](https://hub.docker.com/_/microsoft-dotnet)

