@echo off
REM HDDIndexer Windows Build Script for Windows environments
REM Alternative to the Linux build.sh script for native Windows development

setlocal enabledelayedexpansion

echo HDDIndexer Windows Build Script
echo ================================

REM Check if .NET SDK is installed
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK is not installed or not in PATH
    echo Please install .NET 6 SDK from: https://dotnet.microsoft.com/download
    exit /b 1
)

REM Create output directories
if not exist "build-output" mkdir build-output
if not exist "publish-output" mkdir publish-output
if not exist "msix-output" mkdir msix-output

REM Parse command line arguments
set COMMAND=%1
if "%COMMAND%"=="" set COMMAND=build

if "%COMMAND%"=="build" goto :build
if "%COMMAND%"=="publish" goto :publish
if "%COMMAND%"=="msix" goto :msix
if "%COMMAND%"=="clean" goto :clean
if "%COMMAND%"=="all" goto :all
if "%COMMAND%"=="help" goto :help

echo ERROR: Unknown command: %COMMAND%
goto :help

:build
echo Building HDDIndexer...
dotnet restore src\HDDIndexer\HDDIndexer.csproj
dotnet build src\HDDIndexer\HDDIndexer.csproj -c Release -o build-output
if errorlevel 1 (
    echo ERROR: Build failed
    exit /b 1
)
echo Build completed successfully!
goto :end

:publish
echo Publishing HDDIndexer for deployment...
dotnet restore src\HDDIndexer\HDDIndexer.csproj
dotnet publish src\HDDIndexer\HDDIndexer.csproj -c Release -o publish-output --self-contained true -r win10-x64
if errorlevel 1 (
    echo ERROR: Publish failed
    exit /b 1
)
echo Publish completed successfully!
goto :end

:msix
echo Building MSIX package...
dotnet restore src\HDDIndexer\HDDIndexer.csproj
dotnet publish src\HDDIndexer\HDDIndexer.csproj -c Release -o msix-output -p:GenerateAppxPackageOnBuild=true -p:AppxPackageSigningEnabled=false
if errorlevel 1 (
    echo ERROR: MSIX build failed
    exit /b 1
)
echo MSIX package build completed successfully!
goto :end

:clean
echo Cleaning build artifacts...
if exist "build-output" rmdir /s /q build-output
if exist "publish-output" rmdir /s /q publish-output
if exist "msix-output" rmdir /s /q msix-output
mkdir build-output
mkdir publish-output
mkdir msix-output
echo Build artifacts cleaned.
goto :end

:all
call :clean
call :build
if errorlevel 1 goto :end
call :publish
if errorlevel 1 goto :end
call :msix
goto :end

:help
echo Usage: %0 [COMMAND]
echo.
echo Commands:
echo   build      Build the application (default)
echo   publish    Publish the application for deployment
echo   msix       Create MSIX package for Microsoft Store
echo   clean      Clean build artifacts
echo   all        Run build, publish, and msix
echo   help       Show this help message
echo.
echo Examples:
echo   %0                 # Build the application
echo   %0 build          # Build the application
echo   %0 publish        # Publish for deployment
echo   %0 msix           # Create MSIX package
echo   %0 all            # Build everything
echo   %0 clean          # Clean build artifacts

:end
endlocal