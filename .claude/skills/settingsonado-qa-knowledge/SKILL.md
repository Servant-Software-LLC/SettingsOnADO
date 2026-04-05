---
name: settingsonado-qa-knowledge
description: |
  QA-specific knowledge for testing the SettingsOnADO library. Use when planning test strategies,
  evaluating coverage gaps, writing acceptance criteria, investigating regressions, designing test
  matrices, or assessing release readiness.
  Covers: test inventory, coverage analysis, risk-based testing priorities, edge case catalog,
  schema evolution testing, encryption verification, pub/sub reliability, and test environment setup.

  SELF-UPDATING: When your work changes, advances, or extends testing in SettingsOnADO (new test
  projects, coverage changes, discovered edge cases, resolved defects, etc.), you MUST update this
  skill to reflect the new state before completing your task.
---

# SettingsOnADO QA Knowledge

## Test Inventory

### Test Projects (2 total)

| Project | Scope | Type |
|---------|-------|------|
| SettingsOnADO.Tests | Core library (SettingsManager, SchemaManager, Repository, encryption, pub/sub, cache) | Unit (Moq) + Integration (SQLite) |
| SettingsOnADO.Json.Tests | JSON file-based settings provider | Unit + Integration (file-based) |

### Test Files Detail

**Core Tests (SettingsOnADO.Tests)**:
| File | Focus | Style |
|------|-------|-------|
| SettingsManagerTests.cs | Get/Update contracts | Unit (Moq ISettingsRepository) |
| SettingsManagerIntegrationTests.cs | Full stack round-trips | Integration (SQLite in-memory) |
| SettingsManagerPubSubTests.cs | Subscribe/Unsubscribe/Notify | Integration |
| SettingsManagerWithCacheTests.cs | Cache decorator behavior | Unit (Moq ISettingsManager) |
| SchemaManagerTests.cs | Table creation, column add/drop | Integration (SQLite) |
| SettingsRepositoryTests.cs | Persistence, encryption, schema evolution | Integration (SQLite) |

**JSON Tests (SettingsOnADO.Json.Tests)**:
| File | Focus | Style |
|------|-------|-------|
| JsonSettingsManagerTests.cs | JSON manager, versioning, deprecated properties | Unit |
| JsonSettingsManagerIntegrationTests.cs | File I/O, concurrent access, multi-settings | Integration (file-based) |

### Test Stack
- **Framework**: xUnit 2.9.2
- **Mocking**: Moq 4.18.4
- **Database**: Microsoft.Data.Sqlite 8.0.4 (in-memory)
- **Assertions**: xUnit Assert + FluentAssertions 6.11.0
- **Coverage**: coverlet (XPlat Cobertura), 40-60% threshold

### Running Tests
```powershell
dotnet test --configuration Release
dotnet test --configuration Release --collect:"XPlat Code Coverage"
```

## Feature Test Matrix

### Core SettingsManager

| Feature | Unit Test | Integration Test | Notes |
|---------|-----------|-----------------|-------|
| Get<T> (new settings, creates table) | Y | Y | Verifies schema creation |
| Get<T> (existing settings) | Y | Y | Verifies data retrieval |
| Update<T> (existing table) | Y | Y | Verifies delete-then-insert |
| Update<T> (new table) | Y | Y | Verifies table creation |
| Schema evolution: add column | ? | Y | SchemaManager tests |
| Schema evolution: drop column | ? | Y | SchemaManager tests |
| [Encrypted] attribute | Y | Y | Repository-level tests |
| AesEncryptionProvider | -- | Y | End-to-end encryption |
| DataProtectionEncryptionProvider | -- | ? | May need ASP.NET Core host |
| Pub/sub: Subscribe | -- | Y | Dedicated pub/sub tests |
| Pub/sub: Unsubscribe | -- | Y | Dedicated pub/sub tests |
| Pub/sub: Notify on Update | -- | Y | Verifies OldSettings/NewSettings |
| Cache: Get (cache hit) | Y | -- | Moq-based |
| Cache: Get (cache miss) | Y | -- | Falls through to inner manager |
| Cache: Update (evict + persist) | Y | -- | Moq-based |
| Cache: SetCacheValue | Y | -- | Direct cache manipulation |
| Cache: RemoveCacheValue | Y | -- | Cache eviction |

### JSON Specialization

| Feature | Unit Test | Integration Test | Notes |
|---------|-----------|-----------------|-------|
| Single settings class | Y | Y | One JSON file |
| Multiple settings classes | -- | Y | Separate JSON files |
| Settings versioning (add property) | Y | -- | Default values |
| Settings versioning (remove property) | Y | -- | Graceful ignore |
| [JsonDescription] comments | Y | -- | Comment injection |
| Concurrent read access | -- | Y | Thread safety |
| Custom file path | Y | Y | FileInfo constructor |
| Default path (CommonApplicationData) | Y | -- | Platform-specific |

## Risk-Based Testing Priorities

### P0 -- Data Integrity Risks

1. **Settings data loss on Update**
   - Update uses DELETE-then-INSERT without explicit transaction wrapping
   - If process crashes between DELETE and INSERT, settings row is lost
   - Test: Verify that a failed INSERT after DELETE either rolls back or is recoverable
   - Test: Simulate DbConnection failure mid-Update, verify state

2. **Schema evolution data loss**
   - DROP COLUMN removes data permanently
   - No confirmation or backup before column removal
   - Column rename = drop old + add new (data lost)
   - Test: Add property to settings class, verify old data preserved
   - Test: Remove property, verify remaining properties intact
   - Test: Rename property (different name, same type), verify data state

3. **Encryption round-trip integrity**
   - Encrypted value must decrypt to exact original
   - Key/IV mismatch must fail clearly (not return garbage)
   - [Encrypted] on non-string type must throw
   - Test: Round-trip every supported type through encryption
   - Test: Wrong key → clear error, not silent corruption

### P1 -- Behavioral Correctness Risks

4. **Pub/sub notification reliability**
   - Subscribers must receive OldSettings (before update) and NewSettings (after update)
   - Exception in one subscriber must not prevent other subscribers from being notified
   - Unsubscribed handlers must not receive notifications
   - Test: Multiple subscribers, one throws -- verify others still called
   - Test: Subscribe, Update, Unsubscribe, Update -- verify notification count

5. **Cache coherence**
   - Cache must be invalidated on Update
   - SetCacheValue must not persist to database
   - Get after Update must return updated value (not stale cache)
   - Test: Get (populates cache), Update, Get again -- verify fresh value
   - Test: SetCacheValue, then inner manager Get -- verify DB not changed

6. **Thread safety under concurrent access**
   - Library explicitly states no thread safety guarantees
   - But pub/sub uses ConcurrentDictionary internally -- partial safety
   - SettingsManagerWithCache uses non-concurrent Dictionary
   - Test: Concurrent Get/Update from multiple threads -- characterize behavior
   - Test: Concurrent Subscribe/Unsubscribe -- verify no exceptions

### P2 -- Compatibility & Edge Case Risks

7. **ADO.NET provider compatibility**
   - Core library works with any DbConnection
   - Tested only with SQLite in-memory and JSON file provider
   - SQL Server, PostgreSQL, MySQL -- untested providers
   - Test: If possible, verify Get/Update against other providers

8. **Settings class edge cases**
   - Class with no properties (empty settings)
   - Class with only [Encrypted] properties
   - Class with nullable properties
   - Class with enum properties (stored as string or int?)
   - Class with very long string values
   - Class name that's a SQL reserved word

## Edge Case Catalog

### Schema Evolution
- Add property to existing settings (expect: ALTER TABLE ADD COLUMN, default value in Get)
- Remove property from existing settings (expect: ALTER TABLE DROP COLUMN)
- Change property type (e.g., int → string) -- behavior undefined, likely error
- Add [Encrypted] to previously unencrypted property -- existing plaintext value?
- Remove [Encrypted] from encrypted property -- ciphertext returned as plain string?
- Settings class with zero properties (empty table)

### Encryption
- Encrypt/decrypt empty string
- Encrypt/decrypt very long string (>1MB)
- Encrypt/decrypt string with special characters (null bytes, Unicode)
- Wrong AES key length (not 32 bytes)
- Wrong IV length (not 16 bytes)
- Null encryption provider with [Encrypted] properties -- behavior?
- DataProtection provider with expired/rotated keys

### Pub/Sub
- Subscribe same handler twice
- Unsubscribe handler that was never subscribed
- Subscribe to type A, update type B -- no notification
- Update with identical old/new values -- still notifies?
- Subscriber that takes a long time -- blocks Update return?

### JSON Specialization
- JSON file doesn't exist on Get (expect: create with defaults)
- JSON file exists but is empty `{}`
- JSON file exists but has extra properties (not in class)
- JSON file exists but has missing properties (not all class props)
- JSON file is malformed (invalid JSON)
- JSON file is read-only (OS permission)
- Two JsonSettingsManager instances pointing to same file
- Settings path with spaces, Unicode characters

### Cache
- Get with empty cache (cold start)
- SetCacheValue for type never Get'd
- RemoveCacheValue for type never cached
- Very large cached object (memory pressure)

## Known Fragile Areas

1. **Delete-then-insert atomicity** -- The Update flow in SettingsRepository deletes the existing
   row before inserting the new one. Without transaction wrapping, this is a data loss window.

2. **Schema column ordering** -- Properties are sorted alphabetically and compared against existing
   columns (also sorted). If sorting behavior changes across .NET versions, schema evolution
   could break.

3. **Reflection-based property discovery** -- Settings classes are mapped via reflection
   (typeof(T).GetProperties()). Property ordering from reflection is not guaranteed by the CLR
   spec, though in practice it matches source order.

4. **JSON file concurrent access** -- JsonConnectionEx uses ConcurrentDictionary for type tracking,
   but the underlying JSON file I/O may not be safe for concurrent writes from multiple processes.

## Test Environment Setup

### Prerequisites
- .NET 8 SDK
- No external database required (SQLite in-memory for core, JSON files for JSON tests)

### Test Isolation
- SQLite tests use `Data Source=:memory:` (in-memory, per-connection isolation)
- JSON tests should use unique temp directories per test (sandbox pattern)
- Pub/sub tests should create fresh SettingsManager instances per test

### Test Models
Located in test projects:
- `TestSettings` (Id, Name) -- basic two-property model
- `GeneralSettings`, `AdoProviderSettings` -- more complex models
- `Models/NextVersion/` -- versioned models for evolution testing

## Coverage Gaps to Investigate

1. **No transaction wrapping tests** -- Update's delete-then-insert is not tested for atomicity
2. **No multi-provider tests** -- Only SQLite and JSON providers tested
3. **No concurrent write stress tests** -- Thread safety is disclaimed but not characterized
4. **No DataProtection integration test** -- Only AES encryption is end-to-end tested
5. **No migration/recovery tests** -- What happens after a crash mid-Update?
6. **No performance tests** -- No baselines for Get/Update latency or memory usage
7. **No cross-platform tests** -- CI runs on ubuntu-latest only
8. **Encryption key rotation** -- No tests for re-encrypting data with new keys

## Release Readiness Checklist

- [ ] All test projects pass (`dotnet test --configuration Release`)
- [ ] Code coverage meets threshold (40% yellow, 60% green)
- [ ] Schema evolution tests pass (add/drop column round-trips)
- [ ] Encryption round-trip tests pass (AES end-to-end)
- [ ] Pub/sub notification tests pass (subscribe/notify/unsubscribe)
- [ ] JSON file-based tests pass (create/read/update with file I/O)
- [ ] No new compiler warnings
- [ ] NuGet packages build successfully (`dotnet pack --configuration Release`)
- [ ] Version updated via UpdateVersion.ps1
