#!/usr/bin/env bash
set -euo pipefail

strict_migrations="${NETSTR_DB_CHECK_STRICT_MIGRATIONS:-false}"
if [[ $# -gt 0 ]]; then
  case "$1" in
    --strict-migrations)
      strict_migrations=true
      ;;
    *)
      echo "Usage: $0 [--strict-migrations]" >&2
      exit 1
      ;;
  esac
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
# shellcheck source=./db-common.sh
source "$SCRIPT_DIR/db-common.sh"

load_postgres_context_from_netstr

echo "Checking PostgreSQL connectivity for $PGUSER@$PGHOST:$PGPORT/$PGDATABASE ..."
psql $(postgres_cli_args) -v ON_ERROR_STOP=1 -tAc "SELECT 1" >/dev/null
echo "Connectivity: OK"

migration_table="$(psql $(postgres_cli_args) -tAc "SELECT to_regclass('\"__EFMigrationsHistory\"')")"
migration_table="$(trim "$migration_table")"
if [[ -z "$migration_table" || "$migration_table" == "null" ]]; then
  echo "Migration status: PENDING (table __EFMigrationsHistory not found)."
  if [[ "$strict_migrations" == "true" ]]; then
    echo "Strict mode enabled; treating pending migrations as failure." >&2
    exit 1
  fi

  echo "Migration status: OK (fresh database before first migration run)."
  exit 0
fi

applied_count="$(psql $(postgres_cli_args) -tAc "SELECT COUNT(*) FROM \"__EFMigrationsHistory\"")"
applied_count="$(trim "$applied_count")"
latest_applied="$(psql $(postgres_cli_args) -tAc "SELECT COALESCE(MAX(\"MigrationId\"), '') FROM \"__EFMigrationsHistory\"")"
latest_applied="$(trim "$latest_applied")"

echo "Applied migrations: ${applied_count:-0}"
echo "Latest applied migration: ${latest_applied:-<none>}"

expected_latest=""
migrations_dir="$REPO_ROOT/src/Netstr/Data/Migrations"
if [[ -d "$migrations_dir" ]]; then
  expected_latest="$(find "$migrations_dir" -maxdepth 1 -type f -name '*.cs' ! -name '*.Designer.cs' ! -name '*ModelSnapshot.cs' -printf '%f\n' | sed -E 's/\.cs$//' | sort | tail -n 1)"
fi

if [[ -n "$expected_latest" ]]; then
  echo "Latest known migration in source: $expected_latest"
  if [[ "$latest_applied" != "$expected_latest" ]]; then
    echo "Migration status: OUT_OF_DATE (database is behind source migrations)." >&2
    exit 1
  fi
fi

echo "Migration status: OK"
