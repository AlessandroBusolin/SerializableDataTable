# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SerializableDataTable is a .NET library that provides an abstraction class for serializing and deserializing data to and from `DataTable` instances. The library supports conversion to JSON and markdown formats.

## Build and Development Commands

### Building the Solution
```bash
dotnet build SerializableDataTable.sln
```

### Building Specific Projects
```bash
# Build the main library
dotnet build SerializableDataTable/SerializableDataTables.csproj
```

### Running Tests

Test cases are defined once in `Test.Shared` (the central source of truth) using
[Touchstone](https://www.nuget.org/packages/Touchstone) descriptors, and exposed through three hosts:

```bash
# Touchstone CLI runner (colored tabular output; add "--results results.json" to export)
dotnet run --project Test.Automated/Test.Automated.csproj --framework net8.0

# xUnit adapter
dotnet test Test.Xunit/Test.Xunit.csproj

# NUnit adapter
dotnet test Test.Nunit/Test.Nunit.csproj
```

### Package Management
```bash
# Restore NuGet packages
dotnet restore

# Create NuGet package (automatically done during build due to GeneratePackageOnBuild=true)
dotnet pack SerializableDataTable/SerializableDataTables.csproj
```

## Architecture

### Core Components

- **SerializableDataTable**: Main class that wraps `DataTable` functionality with JSON serialization support
- **SerializableColumn**: Represents column metadata including name and data type
- **ColumnValueTypeEnum**: Enum defining supported data types (String, Int16, Int32, Int64, UInt16, UInt32, UInt64, Decimal, Double, Float, Boolean, DateTime, DateTimeOffset, TimeSpan, Byte, SByte, ByteArray, Char, Guid, Object)
- **MarkdownConverter**: Static utility class for converting tables to markdown format

### Key Features

1. **Bidirectional Conversion**: DataTable ↔ SerializableDataTable
2. **JSON Serialization**: Uses System.Text.Json for serialization
3. **Markdown Export**: Convert tables to markdown format with proper escaping
4. **Type Support**: Handles 13 different data types including nullable values and DBNull
5. **Multi-Target Framework**: Supports .NET Standard 2.0/2.1, .NET 6.0, and .NET 8.0

### Project Structure

- `SerializableDataTable/` - Main library project
- `Test.Shared/` - Central source of truth for all test cases; Touchstone descriptors plus shared assertions
- `Test.Automated/` - Touchstone CLI runner (console host)
- `Test.Xunit/` - Touchstone xUnit adapter (`dotnet test`)
- `Test.Nunit/` - Touchstone NUnit adapter (`dotnet test`)

### Dependencies

- **System.Text.Json 8.0.5** - For JSON serialization (main library)
- **Touchstone 0.1.12** - Runner-agnostic test descriptor framework (test projects)
- **Pgvector 0.3.2** - Used by `Test.Shared` to exercise custom-type reconstruction (no live database required)

## Testing

All test cases live in `Test.Shared/SerializableDataTableScenarios.cs`. Each `public static`,
parameterless method is one atomic test; `SerializableDataTableSuites` groups them into Touchstone
suites by the prefix before the first underscore (e.g. `Markdown_...` → the "Markdown" suite). The
same descriptor collection is run by all three hosts (CLI, xUnit, NUnit), so there is a single place
to add or change coverage.

Coverage spans the entire public surface, positive and negative:
- `SerializableColumn` validation and defaults
- `ColumnValueTypeEnum` JSON string serialization
- `SerializableDataTable` construction, `FromDataTable`, `ToDataTable`
- All 20 data-type mappings and round-trips
- Array type preservation and JSON round-trips (including backward compatibility without `OriginalType`)
- Custom/unknown type reconstruction (Pgvector.Vector)
- Null / DBNull handling
- `MarkdownConverter` (all overloads, escaping, newline configuration, error cases)

To add a test, add a `Prefix_CaseName()` method to `SerializableDataTableScenarios`; it is picked up
automatically by every host.

## NuGet Package

The project is configured for automatic NuGet package generation with version 1.0.3. Package metadata includes proper documentation, licensing, and repository information.

## Coding Standards and Style Rules

**THESE RULES MUST BE FOLLOWED STRICTLY**

### Code Organization

- **Namespace Declaration**: Always at the top, with using statements contained INSIDE the namespace block
- **Using Statement Order**: Microsoft/system libraries first (alphabetical), then other libraries (alphabetical)
- **File Structure**: Limit each file to exactly one class or exactly one enum - no nesting multiple classes/enums

### Documentation Standards

- **Public Members**: All public members, constructors, and public methods MUST have XML documentation
- **Private Members**: NO code documentation on private members or private methods
- **Exception Documentation**: Document exceptions using `/// <exception>` tags
- **Nullability**: Document nullability in XML comments
- **Thread Safety**: Document thread safety guarantees in XML comments
- **Default Values**: Outline default, minimum, maximum values and their effects where appropriate

### Variable and Property Standards

- **No var**: Do not use `var` - use actual types
- **Private Members**: Must start with underscore and be Pascal cased (e.g., `_FooBar`, not `_fooBar`)
- **Public Properties**: Use explicit getters/setters with backing variables when validation is required
- **Configurable Values**: Avoid constants - use public members with backing private members set to reasonable defaults

### Asynchronous Programming

- **ConfigureAwait**: Use `.ConfigureAwait(false)` where appropriate
- **CancellationToken**: Every async method should accept CancellationToken unless class has one as member
- **Cancellation Checks**: Check cancellation requests at appropriate places
- **IEnumerable Methods**: When implementing IEnumerable methods, also create async variants with CancellationToken

### Exception Handling

- **Specific Exceptions**: Use specific exception types rather than generic Exception
- **Meaningful Messages**: Always include meaningful error messages with context
- **Custom Exceptions**: Consider custom exception types for domain-specific errors
- **Exception Filters**: Use when appropriate: `catch (SqlException ex) when (ex.Number == 2601)`

### Resource Management

- **IDisposable**: Implement IDisposable/IAsyncDisposable when holding unmanaged resources
- **Using Statements**: Use 'using' statements or declarations for IDisposable objects
- **Dispose Pattern**: Follow full Dispose pattern with `protected virtual void Dispose(bool disposing)`
- **Base Disposal**: Always call `base.Dispose()` in derived classes

### Null Safety and Validation

- **Nullable Reference Types**: Enable `<Nullable>enable</Nullable>` in project files
- **Input Validation**: Validate parameters with guard clauses at method start
- **Null Checks**: Use `ArgumentNullException.ThrowIfNull()` for .NET 6+ or manual checks
- **Result Pattern**: Consider Result pattern or Option/Maybe types for methods that can fail
- **Proactive Null Safety**: Eliminate situations where null might cause exceptions

### Threading and Concurrency

- **Atomic Operations**: Use Interlocked operations for simple atomic operations
- **Read-Heavy Scenarios**: Prefer ReaderWriterLockSlim over lock for read-heavy scenarios

### LINQ and Collections

- **Readability**: Prefer LINQ methods over manual loops when readability is not compromised
- **Existence Checks**: Use `.Any()` instead of `.Count() > 0`
- **Multiple Enumeration**: Be aware of issues - consider `.ToList()` when needed
- **Safe Access**: Use `.FirstOrDefault()` with null checks rather than `.First()`

### Prohibited Practices

- **No Tuples**: Do not use tuples unless absolutely necessary
- **No Assumptions**: Do not assume class members/methods exist on opaque classes - ask for implementation
- **SQL Statements**: If manual SQL strings exist, assume there's a good reason

### Compilation Requirements

- **Error-Free**: Code must compile without errors or warnings
- **README Accuracy**: If README exists, ensure it remains accurate after changes