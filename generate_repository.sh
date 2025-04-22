#!/bin/bash

# Colors
COLOR_REST='\033[0m'
COLOR_BLUE='\033[0;34m'
COLOR_GREEN='\033[0;32m'
COLOR_RED='\033[0;31m'

# Print the section header
section() {
    echo -e "${COLOR_BLUE}$1${COLOR_REST}"
}

# Print success message
success() {
    echo -e "${COLOR_GREEN}$1${COLOR_REST}"
}

# Print error message
error() {
    echo -e "${COLOR_RED}$1${COLOR_REST}"
}

section "Generating repository manifest..."

# Check if the latest build exists
PLUGIN_NAME="StaticAssets"
VERSION=$(grep -o '"version": "[^"]*"' "Jellyfin.Plugin.StaticAssets/meta.json" | head -1 | awk -F'"' '{print $4}')
FILENAME="${PLUGIN_NAME}_${VERSION}.zip"

if [[ ! -f "dist/${FILENAME}" ]]; then
    error "Build not found: dist/${FILENAME}"
    error "Please run ./build.sh first"
    exit 1
fi

# Calculate SHA256 checksum
CHECKSUM=$(shasum -a 256 "dist/${FILENAME}" | awk '{print $1}')
TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

# Update the version manifest
cat > "repository/versions/staticassets/${VERSION}.json" << EOF
{
    "name": "Static Assets Manager",
    "guid": "1f826750-d8f1-4e44-a814-9432d2833dc0",
    "version": "${VERSION}",
    "targetAbi": "10.8.0.0",
    "framework": "net8.0",
    "overview": "Upload and manage static assets for use in custom JavaScript",
    "description": "A plugin that allows users to upload and manage static assets (images, etc.) that can be used in custom JavaScript injections.",
    "owner": "YourName",
    "repository": "https://github.com/yourusername/jellyfin-static-assets",
    "category": "General",
    "titleColor": "#5271ff",
    "imageUrl": "https://example.com/images/plugin-image.png",
    "changelog": "Initial release"
}
EOF

# Update the main repository manifest
# This is a simplified approach; in a real scenario you might want to parse and update JSON properly
cat > "repository/manifest.json" << EOF
{
    "plugins": [
        {
            "guid": "1f826750-d8f1-4e44-a814-9432d2833dc0",
            "name": "Static Assets Manager",
            "description": "A plugin that allows users to upload and manage static assets (images, etc.) that can be used in custom JavaScript injections.",
            "overview": "Upload and manage static assets for use in custom JavaScript",
            "owner": "YourName",
            "category": "General",
            "versions": [
                {
                    "version": "${VERSION}",
                    "changelog": "Initial release",
                    "targetAbi": "10.8.0.0",
                    "sourceUrl": "https://github.com/yourusername/jellyfin-static-assets/releases/download/v${VERSION}/${FILENAME}",
                    "checksum": "${CHECKSUM}",
                    "timestamp": "${TIMESTAMP}"
                }
            ]
        }
    ]
}
EOF

success "Repository generation completed!"