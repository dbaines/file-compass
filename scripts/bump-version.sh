#!/bin/bash
# Bump version across all source files
# Usage: ./scripts/bump-version.sh 0.2.0

set -e

VERSION=$1

if [ -z "$VERSION" ]; then
  echo "Usage: $0 <version>"
  echo "Example: $0 0.2.0"
  exit 1
fi

# Validate version format (basic semver check)
if ! [[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  echo "Error: Version must be in format X.Y.Z (e.g., 0.2.0)"
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

# Update .csproj
CSPROJ="$ROOT_DIR/src/FileCompass.Desktop/FileCompass.Desktop.csproj"
if [ -f "$CSPROJ" ]; then
  sed -i "s/<Version>.*<\/Version>/<Version>$VERSION<\/Version>/" "$CSPROJ"
  echo "Updated $CSPROJ"
else
  echo "Error: $CSPROJ not found"
  exit 1
fi

# Update AppConstants.cs
CONSTANTS="$ROOT_DIR/src/FileCompass.Core/Constants/AppConstants.cs"
if [ -f "$CONSTANTS" ]; then
  sed -i "s/AppVersion = \".*\"/AppVersion = \"$VERSION\"/" "$CONSTANTS"
  echo "Updated $CONSTANTS"
else
  echo "Error: $CONSTANTS not found"
  exit 1
fi

echo ""
echo "Version bumped to $VERSION"
echo ""
echo "Next steps:"
echo "  1. git commit -am \"Bump version to $VERSION\""
echo "  2. git push"
echo "  3. Run workflow from GitHub Actions with version: $VERSION"
