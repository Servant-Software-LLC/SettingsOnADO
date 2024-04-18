# ServantSoftware.SettingsOnADO.Json

An ADO.NET extension for centralizing application settings in a folder of JSON files. Each setting class corresponds to a JSON file, with its contents only having one instance of the setting class serialized.

![Build Status](https://img.shields.io/badge/build-passing-brightgreen) ![Version](https://img.shields.io/badge/version-1.0.0-blue) ![License](https://img.shields.io/badge/license-MIT-green)

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

### Installation

1. Install the `ServantSoftware.SettingsOnADO.Json` package via NuGet:

```
dotnet add package ServantSoftware.SettingsOnADO.Json
```

| Package Name                   | Release (NuGet) |
|--------------------------------|-----------------|
| `ServantSoftware.SettingsOnADO.Json`       | [![NuGet](https://img.shields.io/nuget/v/ServantSoftware.SettingsOnADO.Json.svg)](https://www.nuget.org/packages/ServantSoftware.SettingsOnADO.Json/)

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
2. Use the Get and Update methods from JsonSettingsManager to retrieve and update settings:
```csharp
var settingsManager = new JsonSettingsManager("MyCompanyName", "MyProductName");

var setting = settingsManager.Get<TestSettings>();
setting.Name = "MyName";
settingsManager.Update(setting);
```
Your application settings will be stored under [Environment.SpecialFolder](https://learn.microsoft.com/en-us/dotnet/api/system.environment.specialfolder?view=net-7.0).CommonApplicationData.  For example on Windows, the default location in this sample will be:
```
C:\ProgramData\MyCompanyName\MyProductName\Settings
```
For more detailed documentation, check our [Wiki](#).

## Contributing

We welcome contributions to SettingsOnADO! Here's how you can help:

1. **Fork** the repository on GitHub.
2. **Clone** your fork locally.
3. **Commit** your changes on a dedicated branch.
4. **Push** your branch to your fork.
5. Submit a **pull request** from your fork to the main repository.
6. Engage in the review process and address feedback.

Please read our [CONTRIBUTING.md](CONTRIBUTING.md) for details on the process and coding standards.

### Issues

Feel free to [submit issues and enhancement requests](https://github.com/Servant-Software-LLC/SettingsOnADO/issues).

## License

SettingsOnADO is licensed under the MIT License. See [LICENSE](LICENSE) for more information.

