# SettingsOnADO Architecture Overview

## Architectural Posture

SettingsOnADO is an ADO.NET extension for centralizing application settings using a single-row-per-table pattern. It wraps any ADO.NET provider (SQLite, SQL Server, JSON files, etc.) and provides typed Get/Update operations with automatic schema evolution, transparent encryption, change notifications, and optional caching.

## Primary Architectural Building Blocks

### Settings Manager Layer
- **SettingsManager**: Core orchestrator implementing ISettingsManager and ISettingsPubSub. Accepts ISettingsRepository or constructs one from DbConnection/ISchemaManager. Coordinates Get/Update with encryption and pub/sub notifications.
- **SettingsManagerWithCache**: Decorator wrapping ISettingsManager. Adds in-memory caching with cache-first reads, evict-on-update behavior, and direct cache manipulation (SetCacheValue/RemoveCacheValue).

### Data Access Layer
- **SettingsRepository**: Implements ISettingsRepository. Maps settings classes to tables using reflection. Handles schema evolution (add/drop columns), data persistence (delete-then-insert), and encryption via [Encrypted] attribute inspection.
- **SchemaManager**: Implements ISchemaManager. Direct SQL operations against the ADO.NET provider for table creation, row retrieval, data insertion/deletion, and column management.

### Encryption Layer
- **IEncryptionProvider**: Strategy interface (Encrypt/Decrypt string methods).
- **AesEncryptionProvider**: System.Security.Cryptography.Aes with manual key/IV. Base64-encoded ciphertext storage.
- **DataProtectionEncryptionProvider**: Microsoft.AspNetCore.DataProtection wrapper with purpose string "SettingsOnADO.Encryption".
- **[Encrypted] Attribute**: Marks string/enum properties for transparent encryption at the repository level.

### Pub/Sub Layer
- **ISettingsPubSub**: Subscribe<T>/Unsubscribe<T> interface for typed change notifications.
- **SettingsPubSub**: Internal implementation using ConcurrentTypeActionCollection.
- **ConcurrentTypeActionCollection**: Thread-safe subscriber tracking per settings type.
- **ConcurrentSet<T>**: Thread-safe delegate collection using ConcurrentDictionary<T, byte>.
- **SettingsChangeEventArgs<T>**: Event args with OldSettings and NewSettings properties.

### JSON Specialization Layer
- **JsonSettingsManager**: File-based alternative storing one JSON file per settings class.
- **JsonConnectionEx**: Custom connection with ConcurrentDictionary for just-in-time type registration.
- **JsonDataSetWriterEx**: Custom writer injecting [JsonDescription] attributes as JSON comments.
- **JsonDescriptionAttribute**: Markup for adding explanatory comments to JSON output.

## Design Patterns

| Pattern | Usage |
|---------|-------|
| Strategy | IEncryptionProvider (AES vs DataProtection), ADO.NET provider choice |
| Decorator | SettingsManagerWithCache wraps ISettingsManager |
| Pub/Sub | ISettingsPubSub for loose-coupled change notifications |
| Repository | ISettingsRepository abstracts data access from business logic |
| Template Method | SchemaManager handles table lifecycle; Repository handles entity mapping |

## Dependency Flow

```
SettingsManager → ISettingsRepository → ISchemaManager → DbConnection
      │                    │
      │                    └── IEncryptionProvider (optional)
      └── SettingsPubSub
              └── ConcurrentTypeActionCollection
                       └── ConcurrentSet<Delegate>
```

## Strengths

1. Any ADO.NET provider works as the settings backend
2. Automatic schema evolution when settings classes change
3. Transparent encryption with pluggable providers
4. Type-safe pub/sub for settings change notifications
5. Clean separation of concerns (Manager/Repository/Schema)
6. JSON specialization for file-based deployments

## Architecture Risks

1. No built-in thread safety (consumer responsibility)
2. Delete-then-insert update strategy is not atomic without transactions
3. Schema evolution is limited to add/drop columns (no rename, type change)
4. SettingsManagerWithCache uses non-concurrent Dictionary
