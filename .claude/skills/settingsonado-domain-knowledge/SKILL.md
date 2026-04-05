---
name: settingsonado-domain-knowledge
description: |
  SettingsOnADO product knowledge for Servant Software LLC agents. Use when working on anything
  related to SettingsOnADO:
  - Product strategy, roadmap, or feature planning
  - Technical architecture discussions or documentation
  - Developer experience, onboarding, or API questions
  - Integration with consuming projects (MockDB, etc.)
  - Understanding current maturity, gaps, or productization needs

  Provides: product purpose, architecture overview, asset inventory, maturity assessment, and productization gaps.

  SELF-UPDATING: When your work changes, advances, or extends SettingsOnADO in ways that affect this
  knowledge (new features, assets, maturity changes, resolved gaps, etc.), you MUST update this skill
  and its reference files to reflect the new state before completing your task. This keeps the knowledge
  accurate for future agents. Update the specific section(s) affected -- do not rewrite unchanged content.
---

# SettingsOnADO Domain Knowledge

## Quick Reference

**What is SettingsOnADO?** An ADO.NET extension for centralizing application settings in a database store. Each settings class corresponds to a table with a single-row configuration. Supports transparent encryption, change notifications (pub/sub), caching, and JSON file-based storage.

## Core Product Facts

- **Primary Interface**: `ISettingsManager` -- Get<T>() / Update<T>() for typed settings
- **Storage Pattern**: Single-row-per-table -- each settings class = one table with one row
- **Schema Evolution**: Automatic ADD/DROP columns when settings classes change
- **Encryption**: Pluggable via IEncryptionProvider (AES and DataProtection built-in)
- **Change Notifications**: Pub/sub with typed SettingsChangeEventArgs<T>
- **Caching**: Decorator pattern (SettingsManagerWithCache)
- **JSON Specialization**: File-based settings via JSON ADO.NET provider
- **Tech Stack**: .NET 8 + netstandard2.0, Microsoft.Data.Sqlite (tests), FileBased.DataProviders (JSON)
- **License**: MIT
- **Distribution**: NuGet (ServantSoftware.SettingsOnADO, ServantSoftware.SettingsOnADO.Json)

## Key Assets

| Asset | Status |
|-------|--------|
| Core SettingsManager | Production (Get/Update, schema evolution, encryption, pub/sub) |
| SettingsManagerWithCache | Production (decorator with in-memory caching) |
| AesEncryptionProvider | Production (256-bit AES, Base64 storage) |
| DataProtectionEncryptionProvider | Production (ASP.NET Core DPAPI integration) |
| [Encrypted] Attribute | Production (transparent string/enum encryption) |
| Pub/Sub Notifications | Production (thread-safe, typed change events) |
| JSON Settings Manager | Production (file-based, one JSON per settings class) |
| [JsonDescription] Attribute | Production (JSON comment injection) |
| Schema Evolution | Production (automatic column add/drop on class changes) |
| NuGet Distribution | 2 packages published |

## Ecosystem Role

SettingsOnADO is a supporting OSS library in Servant Software LLC's product stack:

- **MockDB** -- Uses SettingsOnADO for application configuration persistence
- **FileBased.DataProviders** -- Provides the JSON ADO.NET provider used by SettingsOnADO.Json

## Maturity Assessment

| Dimension | Score | Notes |
|-----------|-------|-------|
| Problem/Solution Fit | 4/5 | Clean abstraction for settings-as-data with schema evolution |
| Core Engineering | 4/5 | Well-separated concerns (Manager/Repository/Schema), decorator/strategy patterns |
| Feature Completeness | 4/5 | CRUD, encryption, pub/sub, caching, JSON specialization |
| Quality & Reliability | 3/5 | Unit + integration tests with SQLite and JSON, pub/sub tests |
| Documentation | 3/5 | README with usage examples, encryption guide; no API reference |
| Commercial Readiness | 3/5 | MIT licensed, NuGet published, CI/CD pipeline |

## Top Gaps

### P0 (Critical)
| Gap | Impact |
|-----|--------|
| No thread safety guarantees | Consumers must implement their own synchronization |

### P1 (Important)
| Gap | Impact |
|-----|--------|
| No API reference documentation | Developer onboarding friction |
| No migration/versioning strategy beyond add/drop columns | Complex schema changes may lose data |
| SettingsManagerWithCache is not concurrent-safe | Single-consumer limitation |

## Detailed References

See [references/](references/) for:
- Architecture Overview
- Asset Inventory
- Maturity Assessment
- Gaps & Next Steps
