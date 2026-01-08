.NET 10 Upgrade Assessment

  Based on my analysis of your project and the official .NET 10 breaking changes, here's the assessment:

  Current State

  - Framework: .NET 9.0
  - UI Framework: Avalonia 11.3.10
  - Key Dependencies: Microsoft.Data.Sqlite 10.0.1, ClosedXML 0.105.0, CommunityToolkit.Mvvm 8.2.1

  Effort Level: Low to Medium

  The upgrade is mostly straightforward version bumps, but there are a few considerations:

  Potential Compatibility Issues

  | Issue                              | Severity  | Description                                                                                                                                        |
  |------------------------------------|-----------|----------------------------------------------------------------------------------------------------------------------------------------------------|
  | Avalonia Framework                 | ⚠️ Medium | Avalonia 11.3.10 targets net8.0. You'll need Avalonia 11.4+ for .NET 10 support - check their releases first                                       |
  | DriveInfo.DriveFormat              | ⚠️ Medium | .NET 10 returns Linux filesystem types (ext4, btrfs, etc.) instead of generic values. Since this is a drive indexer, verify your code handles this |
  | Single-file native library loading | ⚠️ Low    | Single-file apps no longer search executable directory for native libs. SQLite native libs should be fine if bundled correctly                     |

  Non-Issues for Your Project

  - ASP.NET Core changes - Not applicable (desktop app)
  - Windows Forms/WPF changes - Not applicable (using Avalonia)
  - Cryptography changes - Minimal exposure
  - JSON serialization changes - Low risk unless you have complex JSON scenarios

  Files to Modify

  src/FileCompass.Core/FileCompass.Core.csproj          # net9.0 → net10.0
  src/FileCompass.Desktop/FileCompass.Desktop.csproj    # net9.0 → net10.0
  src/FileCompass.Tests/FileCompass.Tests.csproj        # net9.0 → net10.0
  .github/workflows/build.yml                          # 9.0.x → 10.0.x

  Recommended Steps

  1. Wait for/verify Avalonia .NET 10 support (check their GitHub releases)
  2. Update TargetFramework in all 3 .csproj files
  3. Update GitHub Actions to use dotnet-version: '10.0.x'
  4. Run dotnet restore and check for package warnings
  5. Test the DriveInfo.DriveFormat behavior on Linux
  6. Run full test suite and build for both platforms

  Would you like me to proceed with creating an implementation plan, or do you have questions about any specific compatibility concern?

  Sources:
  - https://learn.microsoft.com/en-us/dotnet/core/compatibility/10.0
  - https://learn.microsoft.com/en-us/dotnet/core/compatibility/9.0
