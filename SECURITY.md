# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |

## Reporting a Vulnerability

We take security vulnerabilities seriously. If you discover a security issue, please report it responsibly.

### How to Report

1. **Do NOT open a public issue** for security vulnerabilities
2. Send a detailed report to the maintainers via:
   - GitHub Security Advisories (preferred): Use the "Report a vulnerability" button on the Security tab
   - Email: Create a private report describing the vulnerability

### What to Include

Please provide:
- Description of the vulnerability
- Steps to reproduce
- Potential impact
- Any suggested fixes (optional)

### What to Expect

- **Acknowledgment**: Within 48 hours
- **Initial Assessment**: Within 1 week
- **Resolution Timeline**: Depends on severity, typically 2-4 weeks

### Security Measures in This Project

FileCompass implements several security measures:

#### Path Traversal Prevention
- Backup restoration validates all extracted file paths stay within the target directory
- File scanning validates relative paths don't contain traversal sequences (`..`)

#### Safe Data Handling
- All database queries use parameterized statements (SQL injection prevention)
- DateTime and enum parsing use safe fallback defaults for corrupted data
- JSON deserialization from backup files is validated

#### Resource Management
- Proper disposal of CancellationTokenSource and other IDisposable resources
- Event handler cleanup to prevent memory leaks

### Scope

The following are in scope:
- Code vulnerabilities in FileCompass
- Security issues in dependencies (please report these upstream as well)
- Data exposure risks

The following are out of scope:
- Issues in third-party dependencies that don't affect FileCompass
- Social engineering attacks
- Physical access attacks

## Security Best Practices for Users

1. **Download from official sources**: Only download releases from the official GitHub repository
2. **Verify checksums**: When available, verify release checksums before installation
3. **Keep updated**: Use the latest version to benefit from security fixes
4. **Backup validation**: Be cautious when restoring backups from untrusted sources
5. **File permissions**: Ensure the database file (`catalog.db`) has appropriate permissions

## Acknowledgments

We appreciate security researchers who help keep FileCompass safe. Contributors who report valid security issues will be acknowledged (unless they prefer to remain anonymous).
