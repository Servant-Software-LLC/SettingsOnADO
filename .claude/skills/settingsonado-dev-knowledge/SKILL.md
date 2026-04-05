---
name: settingsonado-dev-knowledge
description: |
  Developer-specific knowledge for working in the SettingsOnADO codebase. Use when implementing features,
  fixing bugs, adding encryption providers, writing tests, or navigating the project structure.
  Covers: solution layout, single-row-per-table pattern, schema evolution, encryption, pub/sub,
  caching decorator, JSON specialization, testing patterns, and practical workflows.

  SELF-UPDATING: When your work changes, advances, or extends SettingsOnADO in ways that affect this
  knowledge (new projects, interfaces, patterns, build steps, etc.), you MUST update this skill to
  reflect the new state before completing your task. This keeps the knowledge accurate for future agents.
  Update the specific section(s) affected -- do not rewrite unchanged content.
---

# SettingsOnADO Developer Knowledge

## Solution Layout

```
src/
  SettingsOnADO/              -- Core ADO.NET settings management library (netstandard2.0 + net8.0)
  SettingsOnADO.Json/         -- JSON file-based settings specialization (netstandard2.0 + net8.0)

tests/
  SettingsOnADO.Tests/        -- Core library tests (unit + integration with SQLite in-memory)
  SettingsOnADO.Json.Tests/   -- JSON provider tests (file-based integration)
```

## Build & SDK

- **SDK**: .NET 7.0 (see global.json) with MSBuild Traversal 3.0.2
- **TFMs**: netstandard2.0 + net8.0 (source projects), net8.0 (test projects)
- **Package pins**: Packages.props centralizes versions (Data.Json 1.0.0.353, EF Core 8.0.11, Sqlite 8.0.4, xUnit 2.9.2, Moq 4.18.4)

### Running Tests
```powershell
# Build
dotnet build --configuration Release

# All tests
dotnet test --configuration Release
```

## Core Architecture

### Class Hierarchy

```
ISettingsManager
  ├─ SettingsManager                -- Core implementation (ADO.NET-backed)
  │    └─ implements ISettingsPubSub
  └─ SettingsManagerWithCache       -- Decorator with in-memory caching
       └─ implements ISettingsManagerWithCache
```

### Key Interfaces

| Interface | Purpose |
|-----------|---------|
| `ISettingsManager` | Get<T>() / Update<T>() -- main settings API |
| `ISchemaManager` | GetRow, CreateTable, InsertTableData, DeleteTableData, AddColumn, DropColumn |
| `ISettingsRepository` | Get<T>() / Update<T>() -- data access abstraction |
| `IEncryptionProvider` | Encrypt(string) / Decrypt(string) |
| `ISettingsPubSub` | Subscribe<T>() / Unsubscribe<T>() -- change notifications |
| `ISettingsManagerWithCache` | SetCacheValue<T>() / RemoveCacheValue<T>() |

### Dependency Flow

```
SettingsManager
  └─ ISettingsRepository (constructor injection)
       └─ ISchemaManager (constructor injection)
            └─ DbConnection (the underlying ADO.NET provider)
```

## Single-Row-Per-Table Pattern

This is the core design pattern of the library:

1. **Each settings class = one database table** (table name = class name)
2. **Each table has exactly one row** of configuration data
3. **Properties map to columns** (sorted alphabetically)
4. **Update = DELETE existing row + INSERT new row** (atomic replacement)

### Schema Evolution

When a settings class changes between versions:
- **New property added**: ALTER TABLE ADD COLUMN automatically
- **Property removed**: ALTER TABLE DROP COLUMN automatically
- Properties are compared sorted alphabetically against existing columns

### Update Flow in SettingsRepository

```
Update<T>(settings)
  1. Get table name from typeof(T).Name
  2. Fetch existing row to check table existence
  3. If table doesn't exist:
     → CreateTable with all properties (sorted)
     → InsertTableData with values
  4. If table exists:
     → Compare properties (sorted) vs columns (sorted)
     → AddColumn for new properties
     → DropColumn for removed properties
     → DeleteTableData (remove existing row)
     → InsertTableData (insert new row)
```

## Encryption

### [Encrypted] Attribute

Mark string/enum properties for transparent encryption:

```csharp
public class AppSettings
{
    public int Id { get; set; }
    [Encrypted]
    public string Password { get; set; }  // Only string/enum types allowed
}
```

### Encryption Providers

| Provider | Key Management | Use Case |
|----------|---------------|----------|
| `AesEncryptionProvider` | Manual (byte[] key + iv) | Standalone apps, cross-platform |
| `DataProtectionEncryptionProvider` | OS-managed via DPAPI | ASP.NET Core apps |

Both implement `IEncryptionProvider` (Encrypt/Decrypt). Values stored as Base64 in the database.

## Pub/Sub Notification System

```
ISettingsPubSub
  └─ Subscribe<T>(Action<SettingsChangeEventArgs<T>>)
  └─ Unsubscribe<T>(Action<SettingsChangeEventArgs<T>>)
```

- **Automatic notification** on `Update<T>()` -- retrieves old settings, calls update, triggers handlers
- **SettingsChangeEventArgs<T>** provides `OldSettings` and `NewSettings`
- **Thread-safe** via ConcurrentTypeActionCollection / ConcurrentSet<T>

## Caching Decorator (SettingsManagerWithCache)

```
SettingsManagerWithCache wraps ISettingsManager
  Get<T>()  → check cache first, fallback to inner manager
  Update<T>() → evict cache, call inner Update, notify subscribers
  SetCacheValue<T>() → cache without DB update, notify subscribers
  RemoveCacheValue<T>() → evict from cache
```

Cache key = fully qualified type name. Not concurrent-safe (designed for single consumer).

## JSON Specialization (SettingsOnADO.Json)

### JsonSettingsManager

File-based alternative to database storage:
- **One JSON file per settings class** (e.g., `GeneralSettings.json`)
- **Default path**: `{CommonApplicationData}/{CompanyName}/{ProductName}/Settings/`
- **Constructor variants**: productName, companyName+productName, FileInfo, FileConnectionString

### Key Classes

| Class | Purpose |
|-------|---------|
| `JsonSettingsManager` | File-based settings manager using JSON ADO.NET provider |
| `JsonConnectionEx` | Custom connection with ConcurrentDictionary for JIT type registration |
| `JsonDataSetWriterEx` | Custom writer supporting [JsonDescription] comment injection |
| `JsonDescriptionAttribute` | Adds JSON comments to properties |

### Versioning Support
- Old properties (deleted from class): Ignored during deserialization
- New properties (added to class): Default values applied if not in JSON
- No breaking changes when evolving settings classes

## Testing Patterns

### Test Organization

| Project | Focus | Database |
|---------|-------|----------|
| SettingsOnADO.Tests | Unit (Moq) + Integration | Microsoft.Data.Sqlite in-memory |
| SettingsOnADO.Json.Tests | Unit + Integration | JSON files (sandbox per test) |

### Test Files

**Core tests**:
- `SettingsManagerTests.cs` -- Unit tests with mocked ISettingsRepository
- `SettingsManagerIntegrationTests.cs` -- Full stack with SQLite
- `SettingsManagerPubSubTests.cs` -- Pub/sub behavior
- `SettingsManagerWithCacheTests.cs` -- Caching decorator
- `SchemaManagerTests.cs` -- Schema creation, column management
- `SettingsRepositoryTests.cs` -- Data persistence, encryption/decryption

**JSON tests**:
- `JsonSettingsManagerTests.cs` -- Unit tests, versioning, deprecated properties
- `JsonSettingsManagerIntegrationTests.cs` -- Full integration with concurrent access

### Test Stack
- **xUnit** for test framework
- **Moq** for mocking (Mock<ISettingsRepository>, Mock<ISchemaManager>)
- **Microsoft.Data.Sqlite** for in-memory integration tests
- **FluentAssertions** (available but xUnit Assert also used)
- **coverlet** for code coverage (XPlat Cobertura format)

### Common Test Pattern
```csharp
[Fact]
public void SettingsRoundTrip_WithEncryption()
{
    var encryptionProvider = new AesEncryptionProvider(key, iv);
    using var connection = new SqliteConnection("Data Source=:memory:");
    connection.Open();
    using var settingsManager = new SettingsManager(connection, true, encryptionProvider);

    var settings = new TestSettings { Id = 1, Password = "secret" };
    settingsManager.Update(settings);
    var retrieved = settingsManager.Get<TestSettings>();

    Assert.Equal("secret", retrieved.Password);
}
```

## Thread Safety

No extra threading safeguards beyond what the ADO.NET provider supplies. Consumers must implement their own synchronization. The pub/sub system uses ConcurrentDictionary internally.

## CI/CD

- **Trigger**: Push/PR to master, manual dispatch
- **Runner**: ubuntu-latest, .NET 8.0.x
- **Version**: 1.0.0.${{github.run_number}} (via UpdateVersion.ps1)
- **Packages published to NuGet.org**: ServantSoftware.SettingsOnADO, ServantSoftware.SettingsOnADO.Json
- **Coverage**: Cobertura XML, 40-60% thresholds, PR comment
