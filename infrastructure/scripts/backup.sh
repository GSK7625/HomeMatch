#!/usr/bin/env bash
set -e

BACKUP_DIR="${BACKUP_DIR:-./backups}"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILE="${BACKUP_DIR}/homematch_db_${TIMESTAMP}.sql.gz"

mkdir -p "$BACKUP_DIR"

echo "=== [HomeMatch] Starting PostgreSQL Database Backup ==="
echo "Target: $BACKUP_FILE"

# Run pg_dump from the running docker container
docker compose exec -T db pg_dump -U "${POSTGRES_USER:-homematch_user}" "${POSTGRES_DB:-homematch_db}" | gzip > "$BACKUP_FILE"

echo "=== [HomeMatch] Backup successfully created at $BACKUP_FILE ==="
# Retain backups for the last 14 days
find "$BACKUP_DIR" -type f -name "homematch_db_*.sql.gz" -mtime +14 -exec rm -f {} \;
echo "Pruned backups older than 14 days."
