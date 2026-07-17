# Architecture notes

## Runtime flow

1. `Program.cs` builds the ASP.NET Core host and loads `appsettings.json` plus
   the optional `data/settings.json` override.
2. Dependency injection registers DNS providers, public-IP providers, storage,
   notifications, health checks, and Quartz jobs.
3. Quartz schedules `UpdateDnsJob` and `BackupConfigJob` from configuration.
4. `UpdateDnsJob` obtains the public IP, reads configured providers/domains,
   updates DNS `A` records, and records the result through `IUpdateStorage`.
5. MVC controllers expose the small history UI; static assets are under
   `DnsUpdater/wwwroot/`.

## Important extension points

- DNS providers: `DnsUpdater/Services/DnsProviders/` implement `IDnsProvider`
  and are registered as keyed services in `Program.cs`.
- Public IP sources: `DnsUpdater/Services/IpProviders/` implement
  `IIpProvider`; `DefaultIpProvider` coordinates the configured sources.
- Configuration models: `DnsUpdater/Models/AppOptions.cs` and related option
  types map configuration sections.
- Persistence: `IUpdateStorage` stores update history in JSON, using paths
  configured under `Settings`.

## Configuration precedence

Runtime configuration is additive: `data/settings.json` overlays values in
the application `appsettings.json` using standard .NET configuration rules.
Avoid changing option names casually because deployed users commonly override
only selected values in `data/settings.json`.

## Deployment

The `Dockerfile` publishes the web app with the .NET 10 Alpine SDK and runs it
on the .NET 10 Alpine ASP.NET runtime image. The repository workflow builds
and publishes `ghcr.io` images from `master` and semantic-version tags.
