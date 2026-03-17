# AGENTS.md

This file provides guidance to AI coding agents (including OpenAI Codex) when working in this repository.

## Repository Overview

SettingsOnADO is a .NET configuration provider that reads application settings from ADO.NET data sources (databases, file-based providers). It bridges Microsoft.Extensions.Configuration with ADO.NET connections, allowing settings to be stored in SQL databases, CSV files, JSON files, and other ADO.NET-compatible sources.

## Review Guidelines

When reviewing pull requests, focus on the following by priority:

### P0 — Must fix (security, data corruption, crashes)
- Exposed connection strings or credentials in code or configuration
- SQL injection in query construction against the settings data source
- Unhandled exceptions during configuration load that crash the application startup
- Incorrect disposal of ADO.NET connections, commands, or readers

### P1 — Should fix (logic bugs, incorrect behavior)
- Settings keys not being normalized consistently (case sensitivity issues)
- Hierarchical key separator handling errors (e.g., `:` vs `__` vs `.`)
- Missing reload support when the underlying data source changes
- Async/await antipatterns (blocking on `.Result`, missing `ConfigureAwait`)
- Thread-safety issues in the configuration provider

### P2 — Nice to fix (skip unless trivial)
- Missing XML doc comments on new public API
- Minor performance improvements in the settings fetch path

## What to Skip
- Code style and formatting
- Refactoring suggestions unrelated to the PR's scope
