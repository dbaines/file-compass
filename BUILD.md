# HDDIndexer Cross-Platform Build Guide

This document explains how to build the HDDIndexer Windows application from Linux using Docker containers.

## Overview

HDDIndexer is a Windows-specific WinUI 3 application that requires Windows SDK and runtime components. This build system allows you to compile Windows executables from a Linux development environment using Docker containers.

## Prerequisites

### Linux Environment
- Docker Desktop with Windows container support
- Git
- Basic shell access

### Windows Container Support
Docker Desktop must be configured to support Windows containers. This requires:
- Docker Desktop for Windows (if running on Windows with WSL2)
- OR Docker with Windows container runtime on Linux

## Project Structure

```
hdd-indexer/
├── src/                          # Source code
│   └── HDDIndexer/
│       ├── HDDIndexer.csproj     # Main project file
│       └── ...                   # Application source files
├── Dockerfile.windows            # Windows container for cross-compilation
├── docker-compose.build.yml      # Build orchestration
├── build.sh                      # Linux build script
├── build.bat                     # Windows build script (native)
├── build-output/                 # Build artifacts
├── publish-output/               # Published deployment files
└── msix-output/                  # MSIX packages for Microsoft Store
```

## Build Methods

### 1. Linux Cross-Compilation (Recommended)

Uses Docker containers to cross-compile Windows applications from Linux.

#### Quick Start
```bash
# Build the application
./build.sh

# Or specify the build type
./build.sh build      # Debug build
./build.sh publish    # Release deployment
./build.sh msix       # Microsoft Store package
./build.sh all        # Build everything
```

#### Available Commands
- `build` - Standard debug/development build
- `publish` - Self-contained deployment package
- `msix` - Microsoft Store package (requires signing for distribution)
- `clean` - Remove all build artifacts
- `all` - Execute build, publish, and msix in sequence
- `help` - Show usage information

### 2. Native Windows Build

For developers working directly on Windows.

```cmd
REM Build the application
build.bat

REM Or specify build type
build.bat build
build.bat publish
build.bat msix
```

### 3. Manual Docker Commands

For advanced users who want direct control over the Docker build process.

```bash
# Build the Docker image
docker build -f Dockerfile.windows -t hddindexer-builder .

# Run specific build types
docker-compose -f docker-compose.build.yml up windows-builder
docker-compose -f docker-compose.build.yml --profile publish up windows-publisher
docker-compose -f docker-compose.build.yml --profile msix up msix-builder
```

## Build Outputs

### build-output/
Contains standard build artifacts:
- `HDDIndexer.exe` - Main executable
- `*.dll` - Required libraries
- `*.pdb` - Debug symbols
- Configuration files

### publish-output/
Contains deployment-ready files:
- Self-contained executable
- All required runtime libraries
- Ready for distribution

### msix-output/
Contains Microsoft Store package:
- `*.msix` - Unsigned package file
- Package manifest
- Assets and metadata

## Docker Configuration Details

### Dockerfile.windows
- Base image: `mcr.microsoft.com/dotnet/sdk:6.0-windowsservercore-ltsc2022`
- Installs Windows App SDK 1.4
- Installs Windows 10/11 SDK components
- Configures cross-compilation environment

### docker-compose.build.yml
Defines three build services:
- `windows-builder` - Standard build
- `windows-publisher` - Deployment build
- `msix-builder` - Microsoft Store package

## Troubleshooting

### Common Issues

#### "Windows containers not available"
```bash
# Switch Docker to Windows containers (Windows host only)
docker context use default
# Or ensure Docker Desktop is configured for Windows containers
```

#### "EnableWindowsTargeting property not set"
This error occurs when trying to build Windows-specific code without proper targeting. The Docker containers automatically set this property.

#### "Access denied" or permission errors
```bash
# Ensure Docker has proper permissions
sudo docker run hello-world

# On Windows, ensure Docker Desktop is running as administrator
```

#### Build artifacts are empty
- Check Docker container logs: `docker-compose logs`
- Verify source code is properly mounted
- Ensure Windows containers are properly configured

### Performance Optimization

#### Build Speed
- Use Docker BuildKit for faster builds: `export DOCKER_BUILDKIT=1`
- Consider using multi-stage builds for repeated compilation
- Cache NuGet packages in Docker volumes

#### Storage Management
```bash
# Clean up Docker images periodically
docker system prune -a

# Remove specific build images
docker rmi hddindexer-builder
```

## Continuous Integration

### GitHub Actions Example
```yaml
name: Build Windows App on Linux

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - name: Build Windows Application
      run: ./build.sh all
    - name: Upload artifacts
      uses: actions/upload-artifact@v3
      with:
        name: hddindexer-builds
        path: |
          build-output/
          publish-output/
          msix-output/
```

### Jenkins Pipeline
```groovy
pipeline {
    agent any
    stages {
        stage('Build') {
            steps {
                sh './build.sh all'
            }
        }
        stage('Archive') {
            steps {
                archiveArtifacts artifacts: '*-output/**/*', fingerprint: true
            }
        }
    }
}
```

## Development Workflow

### Local Development
1. Edit source code on Linux using your preferred editor
2. Use `./build.sh build` for quick builds during development
3. Use `./build.sh publish` for testing deployment packages
4. Use `./build.sh msix` when preparing for distribution

### Testing
- Build artifacts can be tested on Windows machines
- Use Windows VMs or remote Windows systems for runtime testing
- Consider automated testing in Windows containers

## Security Considerations

### Code Signing
- MSIX packages require signing for distribution
- Use Azure Key Vault or hardware security modules for production
- Test with self-signed certificates in development

### Container Security
- Regularly update base Docker images
- Scan containers for vulnerabilities
- Use minimal base images when possible

## Advanced Configuration

### Custom Build Arguments
Modify `docker-compose.build.yml` to add build arguments:
```yaml
args:
  - BUILD_CONFIGURATION=Release
  - TARGET_FRAMEWORK=net6.0-windows10.0.19041.0
```

### Volume Optimization
Use named volumes for better performance:
```yaml
volumes:
  nuget-cache:
    driver: local
```

## Support

### Resources
- [.NET 6 Documentation](https://docs.microsoft.com/en-us/dotnet/core/)
- [WinUI 3 Documentation](https://docs.microsoft.com/en-us/windows/apps/winui/)
- [Docker Windows Containers](https://docs.docker.com/desktop/windows/)

### Known Limitations
- Requires Windows containers (not available on all Linux hosts)
- Build times may be longer than native Windows builds
- Some advanced Windows features may require native builds
- Windows container licensing may apply in production environments

## Contributing

When contributing to the build system:
1. Test changes on both Linux and Windows environments
2. Update this documentation for any new features
3. Ensure backward compatibility with existing workflows
4. Test all build targets (build, publish, msix)