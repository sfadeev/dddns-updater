# dddns updater

Yet another dynamic DNS updater focused on Russian DNS providers.

Inspired by [qdm12/ddns-updater](https://github.com/qdm12/ddns-updater) 

[![Build status](https://github.com/sfadeev/dddns-updater/actions/workflows/docker-publish.yml/badge.svg)](https://github.com/sfadeev/dddns-updater/actions/workflows/docker-publish.yml)
[![Last release](https://img.shields.io/github/v/release/sfadeev/dddns-updater?label=Last%20release)](https://github.com/sfadeev/dddns-updater/releases)

## Features

1. Allow using your domains without having [Static IP](https://en.wikipedia.org/wiki/IP_address#Static_IP)
2. Available as a Docker image at [ghcr.io](https://github.com/sfadeev/dddns-updater/pkgs/container/dddns-updater)
3. Update `A` record of configured domains in `.json` file for supported DNS providers
4. Store IP updates history in `.json` file
5. Simple web UI to view updates history
6. Backup configuration and updates history files
7. **Optional** Send notifications with [Apprise](https://github.com/caronc/apprise)
8. **Optional** Healthcheck with [Healthchecks.io](https://healthchecks.io/)
9. **Optional** Integration with logging systems e.g. [Seq](https://datalust.co/seq)

## Setup

### Container

1. Create a directory for program, e.g. `dddns-updater`
2. In program directory create a directory `data` for settings and updates history files
3. Write a JSON configuration file in `data/settings.json`, for example:
```json
{
  "Settings": {
  	"BaseUrl": "http://192.168.1.1:8080/"
  },
  "Providers": [
    {
      "Provider": "provider",
      "Domains": [
        "my-domain.ru"
      ],
      "Username": "provider-user",
      "Password": "provider-password"
    }
  ]
}
```
4. In program directory write in file `docker-compose.yml`, for example:
```yaml
services:
    dddns-updater:
        container_name: dddns-updater
        image: ghcr.io/sfadeev/dddns-updater:latest
        ports:
            - 8080:8080
        restart: unless-stopped
        volumes:
            - ./data:/app/data
```
5. Run container with command:
```sh
  docker compose up -d
```

## Configuration

Main configuration of service is stored in `appsettings.json` supplied in docker image.
File `data/settings.json` loaded after `appsettings.json` and extends main configuration using [.NET configuration rules](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration).

Start with the following sample content in `data/settings.json`:

```json
{
  "Settings": {
  	"BaseUrl": "http://192.168.1.1:8080/"
  },
  "Providers": [
    {
      "Provider": "provider-1",
      "Domains": [
        "domain-1.ru"
      ],
      "Username": "user-1",
      "Password": "password-1"
    },
    {
      "Provider": "provider-2",
      "Domains": [
        "domain-2.ru"
      ],
      "Username": "user-2",
      "Password": "password-2"
    }
  ]
}
```

### Settings

Common options in section `Settings`

| Option                 | Default               | Description                                           |
|------------------------|-----------------------|-------------------------------------------------------|
| `BaseUrl`              |                       | Base URL for web UI                                   |
| `UpdatesFilePath`      | `./data/updates.json` | File to store updates history                         |
| `BackupDirPath`        | `./data/`             | Directory for backups                                 |
| `BackupFileNamePrefix` | `backup`              | Backup filename prefix                                |
| `MaxUpdatesPerDomain`  | `10`                  | Max records in updates history stored for each domain |
| `MaxBackups`           | `7`                   | Max backup files count                                |


### Providers

For each provider in section `Providers` specify domain(s) and other provider specific parameters

#### Beget

Example configuration

```json
{
  "Providers": [
    {
      "Provider": "beget",
      "Domains": [
        "my-domain.ru", "sub.my-domain.ru" 
      ],
      "Username": "beget-user",
      "Password": "beget-password"
    }
  ]
}
```

| Option   | Required           | Description                                   |
|----------|--------------------|-----------------------------------------------|
| `Username` | :white_check_mark: | Beget username                                |
| `Password` | :white_check_mark: | Beget password                                |

Links

1. Beget API documentation (en) - https://beget.com/en/kb/api/dns-administration-functions
2. Beget API documentation (ru) - https://beget.com/ru/kb/api/funkczii-upravleniya-dns (ru)

#### Timeweb

Example configuration

```json
{
  "Providers": [
    {
      "Provider": "timeweb",
      "Domains": [
        "my-domain.ru", "sub.my-domain.ru" 
      ],
      "Username": "timeweb-user",
      "Password": "timeweb-password",
      "appkey": "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"
    }
  ]
}
```

| Option   | Required           | Description                                               |
|----------|--------------------|-----------------------------------------------------------|
| `Username` | :white_check_mark: | Timeweb username                                          |
| `Password` | :white_check_mark: | Timeweb password                                          |
| `appkey`   | :white_check_mark: | Timeweb API key, should be requested from Timeweb support |

Links

1. Timeweb API documentation (ru) - https://timeweb.com/ru/docs/publichnyj-api-timeweb/metody-api-dlya-virtualnogo-hostinga/#poluchenie-resursnyh-zapisej-domena


#### Reg.ru

> [!WARNING]
> Reg.ru DNS provider implementation not completed, because provider requires to specify static IPs to access API and does not allow access API from dynamic IP address.

Links

1. Reg.ru API documentation (ru) - https://www.reg.ru/reseller/api2doc


#### Rucenter

> [!WARNING]
> Rucenter DNS provider implementation not completed, because provider does not have free plans to access DNS API.

Links

1. Rucenter API documentation (ru) - https://www.nic.ru/help/api-1390/
2. Rucenter app registration - https://www.nic.ru/manager/oauth.cgi
3. Dynamic DNS API - https://www.nic.ru/help/dinamicheskij-dns-dlya-razrabotchikov_4391.html

### Notifications

Service notifies about events (successful DNS updates or errors during updates) using [Apprise](https://github.com/caronc/apprise)

Example configuration to notify to [Telegram bot](https://github.com/caronc/apprise/wiki/Notify_telegram):

```json
{
  "Apprise": {
    "ServiceUrl": "http://192.168.1.101:8008/notify/",
    "NotifyUrls": [
      "tgram://{bot_token}/{chat_id}/"
    ]
  }
}
```

| Option     | Description                                       |
|------------|---------------------------------------------------|
| `ServiceUrl` | URL of deployed Apprise service                   |
| `NotifyUrls` | Collection of notification URLs in Apprise format |

Apprise support 100+ services to notify, for full list see [Apprise Wiki](https://github.com/caronc/apprise/wiki)

### Healthcheck

Example configuration to send healthcheck pings with [Healthchecks.io](https://healthchecks.io/)

```json
{
  "HealthcheckIo": {
    "Url": "https://hc-ping.com/<uuid>"
  } 
}
```

| Option | Description                                                              |
|--------|--------------------------------------------------------------------------|
| `Url`    | Base URL of [Healthcheck.io API](https://healthchecks.io/docs/http_api/) |


### Logging

Service use [Serilog](https://serilog.net/) for logging.

Default logging configuration can be extended in `data/settings.json`, e.g. to integrate with logging services.

Example configuration to integrate with [Seq](https://datalust.co/seq):

```json
{
  "Serilog": {
    "Using":  [ "Serilog.Sinks.Console", "Serilog.Sinks.Seq" ],
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Level:u5}] ({MachineName}/{ThreadId}) {SourceContext} - {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://192.168.1.100:5341",
          "apiKey": "XXXXXXXXXXXXXXXXX",
          "controlLevelSwitch": "$controlSwitch"
        }
      }
    ]
  }
}
```

> [!NOTE]
> Console sink configuration required to write logs to docker.

| Option             | Description                                                                                                                                                        |
|--------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `serverUrl`          | URL of Seq service                                                                                                                                                 |
| `apiKey`             | API key generated in Seq for these service                                                                                                                         |
| `controlLevelSwitch` | Switch to control logging level. Default level specified in `appsettings.json` is `Information`, but it can be controlled from from Seq web UI using these switch. |
