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
6. **Optional** Backup configuration and updates history files
7. **Optional** Send notifications with [Apprise](https://github.com/caronc/apprise)
8. **Optional** Helthcheck with [Healthchecks.io](https://healthchecks.io/)
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
        image: ghcr.io/sfadeev/dddns-updater:v0.2.2
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

| Option  | Default | Description          |
|---------|---------|----------------------|
| BaseUrl |         | Base URL for web UI  |

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
| Username | :heavy_check_mark: | Beget username                                |
| Password | :heavy_check_mark: | Beget password                                |

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
| Username | :heavy_check_mark: | Timeweb username                                          |
| Password | :heavy_check_mark: | Timeweb password                                          |
| appkey   | :heavy_check_mark: | Timeweb API key, should be requested from Timeweb support |

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

### Healthcheck

### Logging

Default logging configuration can be extended in `data/settings.json`, e.g. to integrate with logging services.

Program use [Serilog] to manage logs.

Example configuration to integrate with  [Seq](https://datalust.co/seq):

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
          "serverUrl": "http://192.168.0.100:5341",
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

### Backup
