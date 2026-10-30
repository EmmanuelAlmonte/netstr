#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <version>" >&2
  exit 1
fi

VERSION="$1"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
RUNTIME="${NETSTR_PUBLISH_RUNTIME:-linux-x64}"
RID_LABEL="x86_64-linux-gnu"
DOTNET_CMD="${DOTNET_CMD:-dotnet}"

PUBLISH_DIR="$REPO_ROOT/artifacts/publish/$RUNTIME"
RELEASE_DIR="$REPO_ROOT/artifacts/release"
ASSET_NAME="netstr-${VERSION}-${RID_LABEL}"
ARCHIVE_PATH="$RELEASE_DIR/${ASSET_NAME}.tar.gz"
CHECKSUM_PATH="${ARCHIVE_PATH}.sha256"
STAGE_DIR="$(mktemp -d)"
cleanup() {
  rm -rf "$STAGE_DIR"
}
trap cleanup EXIT

rm -rf "$PUBLISH_DIR"
mkdir -p "$PUBLISH_DIR" "$RELEASE_DIR"

"$DOTNET_CMD" publish "$REPO_ROOT/src/Netstr/Netstr.csproj" \
  --configuration Release \
  --runtime "$RUNTIME" \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:PublishTrimmed=false \
  /p:DebugType=None \
  /p:DebugSymbols=false \
  --output "$PUBLISH_DIR"

binary_source=""
if [[ -f "$PUBLISH_DIR/netstr" ]]; then
  binary_source="$PUBLISH_DIR/netstr"
elif [[ -f "$PUBLISH_DIR/Netstr" ]]; then
  binary_source="$PUBLISH_DIR/Netstr"
else
  echo "Could not find publish output binary in $PUBLISH_DIR" >&2
  exit 1
fi

cp "$binary_source" "$STAGE_DIR/netstr"
chmod +x "$STAGE_DIR/netstr"

tar -C "$STAGE_DIR" -czf "$ARCHIVE_PATH" netstr

archive_layout="$(tar -tzf "$ARCHIVE_PATH" | tr -d '\r')"
if [[ "$archive_layout" != "netstr" ]]; then
  echo "Unexpected archive layout. Expected a single root entry named 'netstr'." >&2
  exit 1
fi

if command -v sha256sum >/dev/null 2>&1; then
  sha256sum "$ARCHIVE_PATH" > "$CHECKSUM_PATH"
else
  shasum -a 256 "$ARCHIVE_PATH" > "$CHECKSUM_PATH"
fi

echo "Created release artifact: $ARCHIVE_PATH"
echo "Created checksum file: $CHECKSUM_PATH"
