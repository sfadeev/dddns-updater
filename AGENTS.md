# AGENTS.md

## Project overview

`dddns-updater` is an ASP.NET Core service that periodically resolves the
current public IP address and updates DNS `A` records through supported
providers. It also stores update history, creates configuration backups, and
can send notifications and health-check pings.

The application targets .NET 10 and is packaged as a Docker image.

For details, see:

- `.agents/architecture.md` for the code map and runtime behaviour.
- `.agents/verification.md` for the usual validation commands.

## Repository layout

- `DnsUpdater/` — web application, background jobs, provider clients and UI.
- `DnsUpdater.Tests/` — NUnit tests.
- `DnsUpdater.sln` — solution entry point.
- `Dockerfile` — multi-stage Alpine image build.
- `data/settings.json` — optional runtime configuration; ignored by Git and
  must never contain example production credentials in committed files.

## Working conventions

- Make focused changes; preserve existing public configuration names and JSON
  shape unless the requested change intentionally introduces a migration.
- Keep provider integrations behind `IDnsProvider` and IP source integrations
  behind `IIpProvider`.
- Add or update NUnit tests for changed behaviour, especially parsing,
  authentication, DNS record selection, and persisted update history.
- Prefer `IHttpClientFactory`-managed clients and existing dependency-injection
  patterns over constructing long-lived `HttpClient` instances directly.
- Do not commit secrets, API keys, passwords, real domains, `data/`, or `.env`
  files. Redact them from logs, examples, test failures, and documentation.
- Do not change release tags, published container images, or GitHub Actions
  credentials unless explicitly requested.

## Configuration and local run

The app loads `DnsUpdater/appsettings.json`, then optionally overlays it with
`./data/settings.json`. Local settings may include DNS-provider credentials,
so keep them outside version control.

Use `DOTNET_ENVIRONMENT=Development` for local development when appropriate.
The app exposes HTTP on port `8080` in the container; `BaseUrl` in runtime
settings should match the externally reachable URL of the web UI.

## Validation

Run the narrowest relevant check first, then use the full suite for code
changes:

```sh
dotnet test DnsUpdater.sln
dotnet build DnsUpdater.sln --no-restore
```

For container changes, also build the image locally:

```sh
docker build -t dddns-updater:local .
```

State clearly if validation was not run and why (for example, unavailable
Docker daemon or package restore/network access).
