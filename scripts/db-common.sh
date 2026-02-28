#!/usr/bin/env bash
set -euo pipefail

die() {
  echo "ERROR: $*" >&2
  exit 1
}

trim() {
  local value="$1"
  value="${value#"${value%%[![:space:]]*}"}"
  value="${value%"${value##*[![:space:]]}"}"
  printf '%s' "$value"
}

load_netstr_env_file() {
  local env_file="${NETSTR_ENV_FILE:-/etc/netstr/netstr.env}"
  if [[ -f "$env_file" ]]; then
    set -a
    # shellcheck disable=SC1090
    source "$env_file"
    set +a
  fi
}

parse_npgsql_connection_string() {
  local connection_string="$1"
  local part key raw_key value normalized_key

  IFS=';' read -r -a parts <<< "$connection_string"
  for part in "${parts[@]}"; do
    part="$(trim "$part")"
    [[ -z "$part" ]] && continue
    [[ "$part" != *=* ]] && continue

    raw_key="${part%%=*}"
    value="${part#*=}"

    key="$(trim "$raw_key")"
    value="$(trim "$value")"
    normalized_key="$(echo "$key" | tr '[:upper:]' '[:lower:]' | tr -d ' ')"

    case "$normalized_key" in
      host)
        PGHOST="$value"
        ;;
      port)
        PGPORT="$value"
        ;;
      database|initialcatalog)
        PGDATABASE="$value"
        ;;
      username|userid|user|uid)
        PGUSER="$value"
        ;;
      password|pwd)
        PGPASSWORD="$value"
        ;;
      sslmode)
        PGSSLMODE="$(echo "$value" | tr '[:upper:]' '[:lower:]')"
        ;;
    esac
  done
}

load_postgres_context_from_netstr() {
  load_netstr_env_file

  local connection_string="${ConnectionStrings__NetstrDatabase:-${NETSTR_DATABASE_URL:-}}"
  [[ -z "$connection_string" ]] && die "ConnectionStrings__NetstrDatabase is not set (env file: ${NETSTR_ENV_FILE:-/etc/netstr/netstr.env})."

  parse_npgsql_connection_string "$connection_string"

  if [[ -n "${PGHOST:-}" && "$PGHOST" == *:* && -z "${PGPORT:-}" ]]; then
    PGPORT="${PGHOST##*:}"
    PGHOST="${PGHOST%%:*}"
  fi

  PGPORT="${PGPORT:-5432}"

  [[ -z "${PGHOST:-}" ]] && die "Could not resolve PGHOST from ConnectionStrings__NetstrDatabase."
  [[ -z "${PGDATABASE:-}" ]] && die "Could not resolve PGDATABASE from ConnectionStrings__NetstrDatabase."
  [[ -z "${PGUSER:-}" ]] && die "Could not resolve PGUSER from ConnectionStrings__NetstrDatabase."

  export PGHOST PGPORT PGDATABASE PGUSER
  if [[ -n "${PGPASSWORD:-}" ]]; then
    export PGPASSWORD
  fi
  if [[ -n "${PGSSLMODE:-}" ]]; then
    export PGSSLMODE
  fi
}

postgres_cli_args() {
  printf -- "--host=%s --port=%s --username=%s --dbname=%s" "$PGHOST" "$PGPORT" "$PGUSER" "$PGDATABASE"
}
