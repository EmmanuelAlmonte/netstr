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

PUBLISH_DIR="$REPO_ROOT/artifacts/publish/$RUNTIME"
RELEASE_DIR="$REPO_ROOT/artifacts/release"
ASSET_NAME="netstr-${VERSION}-${RID_LABEL}"
ASSET_DIR="$RELEASE_DIR/$ASSET_NAME"
ARCHIVE_PATH="$RELEASE_DIR/${ASSET_NAME}.tar.gz"
CHECKSUM_PATH="${ARCHIVE_PATH}.sha256"

rm -rf "$PUBLISH_DIR" "$ASSET_DIR"
mkdir -p "$PUBLISH_DIR" "$ASSET_DIR"

dotnet publish "$REPO_ROOT/src/Netstr/Netstr.csproj" \
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

cp "$binary_source" "$ASSET_DIR/netstr"
chmod +x "$ASSET_DIR/netstr"

tar -C "$RELEASE_DIR" -czf "$ARCHIVE_PATH" "$ASSET_NAME"

if command -v sha256sum >/dev/null 2>&1; then
  sha256sum "$ARCHIVE_PATH" > "$CHECKSUM_PATH"
else
  shasum -a 256 "$ARCHIVE_PATH" > "$CHECKSUM_PATH"
fi

echo "Created release artifact: $ARCHIVE_PATH"
echo "Created checksum file: $CHECKSUM_PATH"
