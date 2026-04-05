# SettingsOnADO Asset Inventory

## Core Product Assets

| Asset Area | Observed Asset | Status |
|------------|----------------|--------|
| Settings Manager | ISettingsManager (Get/Update) with schema evolution | Production |
| Caching Decorator | SettingsManagerWithCache (in-memory, cache-first reads) | Production |
| Schema Manager | ISchemaManager (CreateTable, AddColumn, DropColumn, etc.) | Production |
| Settings Repository | ISettingsRepository (entity-to-table mapping, encryption) | Production |
| AES Encryption | AesEncryptionProvider (256-bit, Base64 storage) | Production |
| DataProtection Encryption | DataProtectionEncryptionProvider (ASP.NET Core DPAPI) | Production |
| [Encrypted] Attribute | Transparent string/enum property encryption | Production |
| Pub/Sub Notifications | ISettingsPubSub with typed SettingsChangeEventArgs<T> | Production |
| JSON Settings Manager | JsonSettingsManager (one JSON file per settings class) | Production |
| [JsonDescription] Attribute | JSON comment injection for documentation | Production |
| Schema Evolution | Automatic column add/drop on settings class changes | Production |

## Test Assets

| Test Type | Projects | Coverage |
|-----------|----------|----------|
| Unit Tests (Moq) | SettingsOnADO.Tests | SettingsManager, Repository, Cache |
| Integration Tests (SQLite) | SettingsOnADO.Tests | Full stack round-trips, encryption, pub/sub |
| JSON Integration Tests | SettingsOnADO.Json.Tests | File-based storage, versioning, concurrent access |

## NuGet Packages

| Package | NuGet ID |
|---------|----------|
| Core Library | ServantSoftware.SettingsOnADO |
| JSON Specialization | ServantSoftware.SettingsOnADO.Json |

## Dependencies

- FileBased.DataProviders (ServantSoftware.Data.Json for JSON specialization)
- Microsoft.AspNetCore.DataProtection (DataProtection encryption)
- Microsoft.Data.Sqlite (integration testing)
- xUnit + Moq + FluentAssertions (testing)
- coverlet (code coverage)
