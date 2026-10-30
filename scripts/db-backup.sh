#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=./db-common.sh
source "$SCRIPT_DIR/db-common.sh"

load_postgres_context_from_netstr

timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
backup_file="${1:-${NETSTR_DB_BACKUP_FILE:-./netstr-${PGDATABASE}-${timestamp}.dump}}"

mkdir -p "$(dirname "$backup_file")"

pg_dump \
  --format=custom \
  --no-owner \
  --no-privileges \
  $(postgres_cli_args) \
  --file="$backup_file"

echo "Backup created: $backup_file"
