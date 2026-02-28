#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=./db-common.sh
source "$SCRIPT_DIR/db-common.sh"

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <backup-file.dump>" >&2
  exit 1
fi

backup_file="$1"
[[ ! -f "$backup_file" ]] && die "Backup file does not exist: $backup_file"

load_postgres_context_from_netstr

pg_restore \
  --clean \
  --if-exists \
  --no-owner \
  --no-privileges \
  $(postgres_cli_args) \
  "$backup_file"

echo "Restore complete from: $backup_file"
