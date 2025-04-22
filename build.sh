#!/bin/bash

VERSION="0.0.1.0"
PLUGIN_NAME="StaticAssets"
PROJECT_DIR="Jellyfin.Plugin.StaticAssets"
CSPROJ_FILE="${PROJECT_DIR}/Jellyfin.Plugin.StaticAssets.csproj"

# Colors
COLOR_REST='\033[0m'
COLOR_BLUE='\033[0;34m'
COLOR_GREEN='\033[0;32m'
COLOR_RED='\033[0;31m'
COLOR_YELLOW='\033[0;33m'

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

# Print warning message
warning() {
    echo -e "${COLOR_YELLOW}$1${COLOR_REST}"
}

# Cleanup previous builds
section "Cleaning previous build artifacts..."
rm -rf dist
rm -rf */bin
rm -rf */obj

# Verify the project file exists
section "Verifying project file..."
if [ -f "$CSPROJ_FILE" ]; then
    success "Project file found: $CSPROJ_FILE"
else
    error "Project file not found: $CSPROJ_FILE"
    error "Cannot continue build. Please ensure the project file exists."
    exit 1
fi

# Make sure the directories exist
section "Ensuring directories..."
mkdir -p "$PROJECT_DIR/Configuration"

# Verify meta.json exists
if [ -f "$PROJECT_DIR/meta.json" ]; then
    success "meta.json found"
else
    error "meta.json not found in expected location!"
    exit 1
fi

# Build the plugin
section "Building the plugin..."
dotnet build "$CSPROJ_FILE" --configuration Release --disable-build-servers --ignore-failed-sources || { error "Build failed!"; exit 1; }

# Create the distribution directory
section "Creating distribution..."
mkdir -p "dist/${PLUGIN_NAME}_${VERSION}"

# Copy the required files
section "Copying files..."
cp "$PROJECT_DIR/bin/Release/net8.0/Jellyfin.Plugin.StaticAssets.dll" "dist/${PLUGIN_NAME}_${VERSION}/Jellyfin.Plugin.StaticAssets.dll" || { error "Failed to copy DLL"; exit 1; }
cp "$PROJECT_DIR/meta.json" "dist/${PLUGIN_NAME}_${VERSION}/meta.json" || { error "Failed to copy meta.json"; exit 1; }

# Print DLL info for verification
section "Verifying DLL..."
echo "DLL Size: $(du -h "dist/${PLUGIN_NAME}_${VERSION}/Jellyfin.Plugin.StaticAssets.dll" | cut -f1)"
echo "DLL Details:"
file "dist/${PLUGIN_NAME}_${VERSION}/Jellyfin.Plugin.StaticAssets.dll"

# Check for embedded resources
section "Checking for embedded resources..."
if command -v monodis &> /dev/null; then
    monodis --resources "dist/${PLUGIN_NAME}_${VERSION}/Jellyfin.Plugin.StaticAssets.dll" || warning "Failed to list embedded resources with monodis"
elif command -v "dotnet-dump" &> /dev/null; then
    # Alternative if we don't have monodis
    warning "monodis not available, skipping resource check"
else
    warning "Resource inspection tools not available, skipping resource check"
fi

# Creating the zip file
section "Creating zip package..."
cd dist
zip -r "${PLUGIN_NAME}_${VERSION}.zip" "${PLUGIN_NAME}_${VERSION}"
cd ..

success "Build completed successfully!"
success "Plugin package is available at: dist/${PLUGIN_NAME}_${VERSION}.zip"
