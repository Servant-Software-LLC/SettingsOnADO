# SettingsOnADO
An ADO.NET extension for centralizing application settings in a database store. Each setting class corresponds to a table, with each table having a single-row configuration.

![Build Status](https://img.shields.io/badge/build-passing-brightgreen) ![Version](https://img.shields.io/badge/version-1.0.0-blue) ![License](https://img.shields.io/badge/license-MIT-green)
![Nuget](https://img.shields.io/nuget/v/ServantSoftware.SettingsOnADO)

A specialized [JSON settings manager](README.Json.md) uses the [JSON ADO.NET Provider](https://github.com/Servant-Software-LLC/FileBased.DataProviders/blob/main/README.Data.Json.md)

## Table of Contents

- [Features](#features)
- [Getting Started](#getting-started)
- [Usage](#usage)
- [Contributing](#contributing)
- [License](#license)

## Features

- **Easy Setup**: Integrate with your existing EF Core projects seamlessly.
- **Centralized Settings**: Store and manage your app settings in a unified manner.
- **Single-row Configurations**: Each setting class corresponds to a table with a unique configuration row.

## Getting Started

### Prerequisites

- .NET Framework 4.6.1/.NET 7 or higher
- An ADO.NET Data Provider to use for storage

### Installation

1. Install the `ServantSoftware.SettingsOnADO` package via NuGet:

```
dotnet add package ServantSoftware.SettingsOnADO
```

| Package Name                   | Release (NuGet) |
|--------------------------------|-----------------|
| `ServantSoftware.SettingsOnADO`       | [![NuGet](https://img.shields.io/nuget/v/ServantSoftware.SettingsOnADO.svg)](https://www.nuget.org/packages/ServantSoftware.SettingsOnADO/)


### Usage
1. Define your settings class with properties of simple data types. Here is an example:
```csharp
namespace SettingsOnADO.Tests.TestClasses;

public class TestSettings
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```
2. Use the Get and Update methods from SettingsManager to retrieve and update settings:
```csharp
// Set up a connection to the ADO.NET Data Provider (as needed)
var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();

// Initialize SchemaManager with the connection
var schemaManager = new SchemaManager(connection);

var setting = settingsManager.Get<TestSettings>();
setting.Name = "MyName";
settingsManager.Update(setting);
```

For more detailed documentation, check our [Wiki](#).

## Using AesEncryptionProvider for Encryption

SettingsOnADO provides a default AES-based encryption provider. Here’s how to use it:

```csharp
using SettingsOnADO;

// Set up a connection to the ADO.NET Data Provider (as needed)
var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();

// Define key and IV (ensure proper key length for AES)
byte[] key = Encoding.UTF8.GetBytes("your-256-bit-key"); // 32 bytes
byte[] iv = Encoding.UTF8.GetBytes("your-128-bit-iv");   // 16 bytes

var encryptionProvider = new AesEncryptionProvider(key, iv);
var settingsManager = new SettingsManager(connection, true, encryptionProvider);

// Now you can use settingsManager to get and update settings with encryption.
```

## Example: Using DataProtectionProvider for Encryption

Here is an example of how to use `DataProtectionProvider` with the `SettingsOnADO` library:

```csharp
using Microsoft.AspNetCore.DataProtection;
using SettingsOnADO;

// Set up a connection to the ADO.NET Data Provider (as needed)
var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();

var dataProtectionProvider = DataProtectionProvider.Create("SettingsOnADO");
var encryptionProvider = new DataProtectionEncryptionProvider(dataProtectionProvider);
var settingsManager = new SettingsManager(connection, true, encryptionProvider);

// Now you can use settingsManager to get and update settings with encryption.
```

## Contributing

We welcome contributions to SettingsOnADO! Here's how you can help:

1. **Fork** the repository on GitHub.
2. **Clone** your fork locally.
3. **Commit** your changes on a dedicated branch.
4. **Push** your branch to your fork.
5. Submit a **pull request** from your fork to the main repository.
6. Engage in the review process and address feedback.

Please read our [CONTRIBUTING.md](CONTRIBUTING.md) for details on the process and coding standards.

## Thread Safety Policy

No extra threading safeguards have been put in place, other than what is provided by the ADO.NET Data Provider that is used.  It is up to the consumer of this library to put proper synchronization in place.


### Issues

Feel free to submit issues and enhancement requests.

## License

SettingsOnADO is licensed under the MIT License. See [LICENSE](LICENSE) for more information.

