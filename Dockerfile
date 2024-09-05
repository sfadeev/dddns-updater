FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG TARGETARCH
WORKDIR /source

# COPY Directory.Packages.props DnsUpdater/*.csproj .
COPY DnsUpdater/*.csproj .
RUN dotnet restore -a $TARGETARCH

COPY DnsUpdater/. .
RUN dotnet publish --no-restore -a $TARGETARCH -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
EXPOSE 8080
WORKDIR /app
COPY --from=build /app .
# Uncomment to enable non-root user
# USER $APP_UID
ENTRYPOINT [ "./DnsUpdater" ]