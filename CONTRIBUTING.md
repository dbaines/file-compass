# Contributing to FileCompass

Thank you for your interest in contributing to FileCompass! This document provides guidelines and information to make the contribution process smooth and effective.

## Code of Conduct

By participating in this project, you agree to maintain a respectful and inclusive environment. Please be considerate in your interactions with other contributors.

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- A code editor (Visual Studio, VS Code with C# extension, JetBrains Rider, etc.)
- Git

### Setting Up the Development Environment

1. **Fork the repository** on GitHub

2. **Clone your fork**:
   ```bash
   git clone https://github.com/YOUR_USERNAME/hdd-indexer-universal.git
   cd hdd-indexer-universal
   ```

3. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

4. **Build the project**:
   ```bash
   dotnet build
   ```

5. **Run the tests**:
   ```bash
   dotnet test
   ```

6. **Run the application**:
   ```bash
   dotnet run --project src/FileCompass.Desktop
   ```

## Project Structure

```
hdd-indexer-universal/
├── src/
│   ├── FileCompass.Core/      # Business logic, models, data access, services
│   ├── FileCompass.Desktop/   # Avalonia UI desktop application
│   └── FileCompass.Tests/     # Unit tests
├── .github/
│   └── workflows/            # CI/CD pipelines
└── plan/                     # Development planning documents
```

## How to Contribute

### Reporting Bugs

Before submitting a bug report:
- Check existing issues to avoid duplicates
- Gather relevant information about your environment

When submitting a bug report, include:
- Operating system and version
- .NET version (`dotnet --version`)
- Steps to reproduce the issue
- Expected vs actual behavior
- Screenshots if applicable
- Error messages or logs

### Suggesting Features

Feature suggestions are welcome! Please:
- Check existing issues and discussions first
- Clearly describe the use case and benefits
- Consider how it fits with the project's goals

### Pull Requests

1. **Create a branch** for your changes:
   ```bash
   git checkout -b feature/your-feature-name
   # or
   git checkout -b fix/your-bug-fix
   ```

2. **Make your changes** following the coding guidelines below

3. **Write or update tests** for your changes

4. **Ensure all tests pass**:
   ```bash
   dotnet test
   ```

5. **Build without warnings**:
   ```bash
   dotnet build --configuration Release
   ```

6. **Commit your changes** with a clear, descriptive message:
   ```bash
   git commit -m "Add feature: description of the feature"
   # or
   git commit -m "Fix: description of the bug fix"
   ```

7. **Push to your fork**:
   ```bash
   git push origin feature/your-feature-name
   ```

8. **Open a Pull Request** against the `main` branch

## Coding Guidelines

### General Principles

- Write clear, readable code
- Keep changes focused and minimal
- Avoid over-engineering
- Follow existing patterns in the codebase

### Code Style Enforcement

This project uses:
- **`.editorconfig`** - Enforces consistent code style across all editors
- **Meziantou.Analyzer** - Additional code quality rules (configured in `Directory.Build.props`)
- **EnableNETAnalyzers** - Built-in .NET code analysis

Your IDE should automatically apply formatting rules from `.editorconfig`. Key conventions:
- File-scoped namespaces
- Prefer `var` when type is apparent
- Private fields use `_camelCase` naming
- Async methods end with `Async` suffix
- 4-space indentation

### C# Style

- Use meaningful names for variables, methods, and classes
- Use `async`/`await` for asynchronous operations
- Prefer LINQ for collection operations where it improves readability
- Use nullable reference types appropriately

### Avalonia UI (Desktop)

- Follow MVVM pattern
- Keep views focused on presentation
- Place business logic in ViewModels or Core services
- Use data binding over code-behind where practical

### Commit Messages

- Use present tense ("Add feature" not "Added feature")
- Use imperative mood ("Fix bug" not "Fixes bug")
- Keep the first line under 72 characters
- Reference issues when applicable (`Fixes #123`)

### Testing

- Write unit tests for new functionality
- Test edge cases and error conditions
- Tests should be independent and repeatable
- Use descriptive test method names

## Building for Different Platforms

### Current Platform
```bash
dotnet publish src/FileCompass.Desktop -c Release -o publish
```

### Self-Contained Builds
```bash
# Linux x64
dotnet publish src/FileCompass.Desktop -c Release -r linux-x64 --self-contained -o publish/linux-x64

# Windows x64
dotnet publish src/FileCompass.Desktop -c Release -r win-x64 --self-contained -o publish/win-x64

# macOS x64
dotnet publish src/FileCompass.Desktop -c Release -r osx-x64 --self-contained -o publish/osx-x64

# macOS ARM64
dotnet publish src/FileCompass.Desktop -c Release -r osx-arm64 --self-contained -o publish/osx-arm64
```

## Continuous Integration

All pull requests are automatically built and tested via GitHub Actions. Ensure your changes:
- Build successfully on all platforms
- Pass all existing tests
- Don't introduce new warnings

## Getting Help

If you have questions:
- Check existing issues and documentation
- Open a new issue for discussion
- Be patient and respectful when waiting for responses

## Recognition

Contributors will be recognized in release notes. Thank you for helping improve FileCompass!

## License

By contributing to FileCompass, you agree that your contributions will be licensed under the [MIT License](LICENSE).
