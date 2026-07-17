# Verification guide

Run commands from the repository root.

## Normal code change

```sh
dotnet restore DnsUpdater.sln
dotnet build DnsUpdater.sln --no-restore
dotnet test DnsUpdater.sln --no-restore
```

## Focused test run

```sh
dotnet test DnsUpdater.Tests/DnsUpdater.Tests.csproj --filter "FullyQualifiedName~NameOfTestOrFixture"
```

## Local application run

Create `data/settings.json` locally with non-production or redacted provider
credentials, then run:

```sh
dotnet run --project DnsUpdater/DnsUpdater.csproj
```

Do not point a development run at production DNS credentials unless the change
is explicitly intended to perform a real DNS update.

## Container check

```sh
docker build -t dddns-updater:local .
docker run --rm -p 8080:8080 -v "$(pwd)/data:/app/data" dddns-updater:local
```

The mounted `data/` directory is intentionally ignored by Git. Stop the
container after the smoke check; its scheduled jobs can make external calls.
