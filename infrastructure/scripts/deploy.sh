#!/usr/bin/env bash
set -e

echo "=== [HomeMatch] Starting Deployment ==="

# Check environment file
if [ ! -f .env ]; then
    echo "Warning: .env file not found. Copying from .env.example..."
    cp .env.example .env
fi

# Pull latest git changes if on a server
if [ -d .git ]; then
    echo "Pulling latest changes from git repository..."
    git pull origin main
fi

# Build and restart containers using Docker Compose
echo "Rebuilding and restarting docker containers..."
docker compose pull || true
docker compose up -d --build --remove-orphans

echo "Waiting for services to become healthy..."
docker compose ps

echo "=== [HomeMatch] Deployment Completed Successfully! ==="
