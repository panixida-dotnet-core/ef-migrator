# PANiXiDA.Core.Ef.Migrator

`PANiXiDA.Core.Ef.Migrator` is a .NET library for automatically creating and applying Entity Framework Core migrations when an application starts.

It is designed for services that use PostgreSQL with EF Core and need a controlled startup-time migration flow for development, test, or managed deployment scenarios.

## Status

[![CI](https://github.com/panixida-dotnet-core/ef-migrator/actions/workflows/ci.yml/badge.svg)](https://github.com/panixida-dotnet-core/ef-migrator/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/PANiXiDA.Core.Ef.Migrator.svg)](https://www.nuget.org/packages/PANiXiDA.Core.Ef.Migrator)
[![NuGet downloads](https://img.shields.io/nuget/dt/PANiXiDA.Core.Ef.Migrator.svg)](https://www.nuget.org/packages/PANiXiDA.Core.Ef.Migrator)
[![Target Framework](https://img.shields.io/badge/target-net10.0-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/github/license/panixida-dotnet-core/ef-migrator.svg)](LICENSE)

## Overview

The package extends `IHostBuilder` with a migration startup step. It builds the host, resolves the configured `DbContext`, detects pending model changes, optionally scaffolds a new EF Core migration into the target project, and optionally applies existing and generated migrations to PostgreSQL.

This package is intentionally small: the public entry point is `RunMigrationsAsync<TContext>()`, while generation and application behavior is controlled through configuration.

## Features

- Detects differences between the current `DbContext` model and the latest model snapshot.
- Generates migration files into a configured project directory.
- Applies compiled pending migrations.
- Applies a newly generated migration in the same startup flow.
- Supports disabling generation and applying independently.
- Uses PostgreSQL through `Npgsql.EntityFrameworkCore.PostgreSQL`.

## Quick Start

### Requirements

- .NET 10 SDK
- Entity Framework Core 10
- PostgreSQL

### Installation

```xml
<ItemGroup>
  <PackageReference Include="PANiXiDA.Core.Ef.Migrator" Version="1.0.1" />
</ItemGroup>
```

### Minimal import

```csharp
using PANiXiDA.Core.Ef.Migrator;
```

### First example

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using PANiXiDA.Core.Ef.Migrator;

var builder = Host
    .CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(
                context.Configuration["PostgreSqlConnectionString"],
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name));
        });
    });

using var host = await builder.RunMigrationsAsync<AppDbContext>();
await host.RunAsync();
```

## Usage

### Configuration

The migration flow is controlled by these configuration values:

| Key | Required | Default | Description |
| --- | --- | --- | --- |
| `GenerateMigrations` | No | `true` | Enables creating a migration when the model differs from the latest snapshot. |
| `ApplyMigrations` | No | `true` | Enables applying compiled pending migrations and the newly generated migration. |
| `Ef:ProjectPath` | Yes, when generation is enabled and differences exist | None | Absolute or relative path to the project where migration files should be written. |
| `Ef:MigrationsDirectory` | Yes, when generation is enabled and differences exist | None | Migration output directory inside `Ef:ProjectPath`. |

Example `appsettings.json`:

```json
{
  "GenerateMigrations": true,
  "ApplyMigrations": true,
  "Ef": {
    "ProjectPath": ".",
    "MigrationsDirectory": "Data/Migrations"
  },
  "PostgreSqlConnectionString": "Host=localhost;Database=app;Username=app;Password=app"
}
```

### Generate but do not apply

```json
{
  "GenerateMigrations": true,
  "ApplyMigrations": false,
  "Ef": {
    "ProjectPath": ".",
    "MigrationsDirectory": "Data/Migrations"
  }
}
```

### Apply existing migrations only

```json
{
  "GenerateMigrations": false,
  "ApplyMigrations": true
}
```

### Disable startup migrations

```json
{
  "GenerateMigrations": false,
  "ApplyMigrations": false
}
```

## Behavior Notes

- `RunMigrationsAsync<TContext>()` returns the built `IHost`.
- When generation is disabled and applying is enabled, only compiled pending migrations are applied.
- When generation is enabled but there are no model differences, the package applies compiled migrations with EF Core `MigrateAsync()` if applying is enabled.
- When generation and applying are both disabled, the host is built and returned without migration work.
- `Ef:MigrationsDirectory` must point to a directory inside `Ef:ProjectPath`.

## Project Structure

```text
.
|-- src/
|   `-- PANiXiDA.Core.Ef.Migrator/
|-- tests/
|   `-- PANiXiDA.Core.Ef.Migrator.IntegrationTests/
|-- .github/
|   `-- workflows/
|       `-- ci.yml
|-- Directory.Build.props
|-- Directory.Build.targets
|-- Directory.Packages.props
|-- global.json
|-- version.json
|-- LICENSE
`-- README.md
```

## Development

### Build

```bash
dotnet restore
dotnet build --configuration Release
```

### Format

```bash
dotnet format
```

### Test

```bash
dotnet test --configuration Release
```

Integration tests use Testcontainers and require a working Docker environment.

### Pack

```bash
dotnet pack --configuration Release
```

### Full local validation

```bash
dotnet restore
dotnet format
dotnet build --configuration Release
dotnet test --configuration Release
dotnet pack --configuration Release
```

## Tooling and Conventions

This repository uses:

- .NET 10
- Nullable enabled
- Implicit usings enabled
- Central package management
- Microsoft Testing Platform
- xUnit v3
- FluentAssertions
- Testcontainers for PostgreSQL integration tests
- Nerdbank.GitVersioning
- GitHub Actions

## License

This project is licensed under the Apache-2.0 license.

See the [LICENSE](LICENSE) file for details.

## Maintainers

Maintained by PANiXiDA.

For questions or improvements, use GitHub Issues or Pull Requests.
