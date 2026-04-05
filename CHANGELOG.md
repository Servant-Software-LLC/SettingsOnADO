# Changelog

All notable changes to SettingsOnADO will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Comprehensive integration test coverage for consumer workflows
- Concurrent access tests for thread-safety validation
- Integration tests for encryption, edge cases, and interoperability

### Fixed
- **Thread Safety**: Migrated JSON settings type cache to `ConcurrentDictionary` and replaced unsafe check-then-add pattern with atomic `GetOrAdd()` for concurrent access
- **Null Property Handling**: SettingsRepository now correctly stores nullable property values as `DBNull.Value` instead of throwing `ArgumentNullException`

### Changed
- **BREAKING CHANGE**: `SettingsRepository.GetPersistedPropertyValue()` (formerly `GetEncryptedPropertyValue()`) now returns `DBNull.Value` for null values in nullable properties instead of throwing an exception
  - This enables proper handling of nullable properties (e.g., `string?`, `int?`)
  - Consuming code that depends on the exception behavior should be updated to handle null values appropriately
  - This change is a correction to properly support nullable reference types

### Removed
- Removed overly-strict null validation checks that prevented nullable properties from being null
