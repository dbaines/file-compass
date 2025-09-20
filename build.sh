#!/bin/bash

# HDDIndexer Windows Cross-Compilation Build Script for Linux
# This script uses Docker to build Windows applications from Linux

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to check if Docker is running
check_docker() {
    if ! docker info >/dev/null 2>&1; then
        print_error "Docker is not running. Please start Docker and try again."
        exit 1
    fi

    # Check if Windows containers are supported
    if ! docker system info | grep -q "OSType.*windows" 2>/dev/null; then
        print_warning "Windows containers may not be available. Attempting to switch..."
        docker system info | grep "OSType" || true
    fi
}

# Function to create output directories
create_directories() {
    print_status "Creating output directories..."
    mkdir -p build-output
    mkdir -p publish-output
    mkdir -p msix-output
}

# Function to clean previous builds
clean_build() {
    print_status "Cleaning previous build artifacts..."
    rm -rf build-output/*
    rm -rf publish-output/*
    rm -rf msix-output/*
}

# Function to build the application
build_app() {
    print_status "Building HDDIndexer Windows application..."

    docker-compose -f docker-compose.build.yml up --build windows-builder

    if [ $? -eq 0 ] && [ -d "build-output" ] && [ "$(ls -A build-output)" ]; then
        print_success "Build completed successfully!"
        print_status "Build artifacts are in: $(pwd)/build-output"
        ls -la build-output/
    else
        print_error "Build failed or no output generated."
        return 1
    fi
}

# Function to publish the application
publish_app() {
    print_status "Publishing HDDIndexer Windows application for deployment..."

    docker-compose -f docker-compose.build.yml --profile publish up --build windows-publisher

    if [ $? -eq 0 ] && [ -d "publish-output" ] && [ "$(ls -A publish-output)" ]; then
        print_success "Publish completed successfully!"
        print_status "Published artifacts are in: $(pwd)/publish-output"
        ls -la publish-output/
    else
        print_error "Publish failed or no output generated."
        return 1
    fi
}

# Function to create MSIX package
build_msix() {
    print_status "Building MSIX package for Microsoft Store deployment..."

    docker-compose -f docker-compose.build.yml --profile msix up --build msix-builder

    if [ $? -eq 0 ] && [ -d "msix-output" ] && [ "$(ls -A msix-output)" ]; then
        print_success "MSIX package build completed successfully!"
        print_status "MSIX artifacts are in: $(pwd)/msix-output"
        ls -la msix-output/
    else
        print_error "MSIX build failed or no output generated."
        return 1
    fi
}

# Function to show usage
show_usage() {
    echo "Usage: $0 [COMMAND]"
    echo ""
    echo "Commands:"
    echo "  build      Build the application (default)"
    echo "  publish    Publish the application for deployment"
    echo "  msix       Create MSIX package for Microsoft Store"
    echo "  clean      Clean build artifacts"
    echo "  all        Run build, publish, and msix"
    echo "  help       Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0                 # Build the application"
    echo "  $0 build          # Build the application"
    echo "  $0 publish        # Publish for deployment"
    echo "  $0 msix           # Create MSIX package"
    echo "  $0 all            # Build everything"
    echo "  $0 clean          # Clean build artifacts"
}

# Main script logic
main() {
    print_status "HDDIndexer Windows Cross-Compilation Build Script"
    print_status "=================================================="

    # Check prerequisites
    check_docker
    create_directories

    # Parse command line arguments
    COMMAND=${1:-build}

    case $COMMAND in
        "build")
            clean_build
            build_app
            ;;
        "publish")
            clean_build
            publish_app
            ;;
        "msix")
            clean_build
            build_msix
            ;;
        "clean")
            clean_build
            print_success "Build artifacts cleaned."
            ;;
        "all")
            clean_build
            build_app && publish_app && build_msix
            ;;
        "help"|"-h"|"--help")
            show_usage
            ;;
        *)
            print_error "Unknown command: $COMMAND"
            show_usage
            exit 1
            ;;
    esac
}

# Run main function
main "$@"