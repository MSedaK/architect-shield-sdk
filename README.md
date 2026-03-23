# ArchitectShield SDK

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Status: Development](https://img.shields.io/badge/Status-Active_Development-orange.svg)]()

**ArchitectShield** is an automated code quality enforcement and refactoring engine for enterprise C# projects, built on top of the .NET Compiler Platform (Roslyn).

Instead of relying on manual code reviews to enforce architectural guidelines, ArchitectShield acts as a real-time guardian inside your IDE. It analyzes your syntax trees on the fly and provides automatic `CodeFix` providers to transform legacy or non-compliant code into clean, secure, and standardized structures.

## Core Capabilities

* **Real-Time Diagnostics:** Analyzes syntax and semantic models to detect architecture violations.
* **Syntax Transformation:** Automatically rewrites code blocks using `SyntaxFactory` for zero-friction refactoring.
* **Security & Hardcode Prevention:** Detects sensitive literals (IPs, connection strings) and routes them to configuration management.
* **Pattern Enforcement:** Enforces the `Result<T>` pattern and `Async` naming conventions across the workspace.

## Refactoring Example

ArchitectShield doesn't just warn you; it writes the boilerplate for you.

**Before (Non-compliant code):**
```csharp
public class DataService
{
    public Task<Data> FetchSystemData()
    {
        string connStr = "Server=192.168.1.50;Database=Prod;";
        return _repository.GetData(connStr);
    }
}
```

**After (ArchitectShield applied CodeFix):**
```csharp
public class DataService
{
    public async Task<Result<Data>> FetchSystemDataAsync()
    {
        try
        {
            var connStr = ConfigurationManager.AppSettings["DB_CONNECTION"];
            var data = await _repository.GetDataAsync(connStr);
            
            return Result.Success(data);
        }
        catch (Exception ex)
        {
            // Log integration can be configured
            return Result.Failure<Data>(ex.Message);
        }
    }
}
```

## System Architecture

ArchitectShield is designed with a highly modular, multi-layered architecture:

1. **Analysis Engine (`DiagnosticAnalyzer`):** Scans `SyntaxNodes` (e.g., `StringLiteralExpression`, `MethodDeclaration`) and queries the `SemanticModel` to yield precise diagnostics.
2. **Transformation Engine (`CodeFixProvider`):** Consumes diagnostics and utilizes `SyntaxFactory` and `DocumentEditor` to safely mutate the syntax tree and update workspace references.
3. **Resource Provider:** A caching layer that acts as a mock Label Management System, syncing hardcoded strings with a centralized `labels.json` dictionary.

## Modules

| Module | Diagnostic Role | CodeFix Action |
| :--- | :--- | :--- |
| **Label Automator** | Detects hardcoded strings present in the dictionary. | Replaces literal with `LabelService.Get(KEY)`. |
| **Security Guard** | Identifies IP addresses and DB credentials via Regex. | Extracts values to environment/config wrappers. |
| **Async Standardizer** | Finds `Task`-returning methods missing the suffix. | Renames method and updates all solution references. |
| **Safe Wrapper** | Targets raw methods lacking proper exception handling. | Injects `try-catch` blocks and wraps returns in `Result<T>`. |

## Project Structure

```text
ArchitectShield.sln
├── src/
│   ├── ArchitectShield.Analyzers     # Core diagnostic logic
│   ├── ArchitectShield.CodeFixes     # Syntax rewriting logic
│   └── ArchitectShield.VSIX          # Visual Studio extension packaging
├── tests/
│   └── ArchitectShield.Tests         # Roslyn unit testing framework
└── docs/
```

## License

Distributed under the MIT License. See `LICENSE` for more information.
