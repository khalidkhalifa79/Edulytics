ARG DOTNET_SDK_VERSION=10.0.200

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION} AS build

WORKDIR /src

COPY global.json Directory.Build.props Edulytics.sln ./
COPY .config/dotnet-tools.json .config/dotnet-tools.json

COPY src/Edulytics.Core/Edulytics.Core.csproj \
     src/Edulytics.Core/
COPY src/Edulytics.Services/Edulytics.Services.csproj \
     src/Edulytics.Services/
COPY src/Edulytics.Data/Edulytics.Data.csproj \
     src/Edulytics.Data/
COPY src/Edulytics.Web/Edulytics.Web.csproj \
     src/Edulytics.Web/

RUN dotnet restore \
    src/Edulytics.Web/Edulytics.Web.csproj

RUN dotnet tool restore

COPY src/ src/

RUN dotnet publish \
    src/Edulytics.Web/Edulytics.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

RUN EDULYTICS_CONNECTION_STRING="Host=localhost;Port=5432;Database=edulytics_build;Username=postgres;Password=build-only;SSL Mode=Disable" \
    dotnet ef migrations bundle \
    --project src/Edulytics.Data/Edulytics.Data.csproj \
    --startup-project src/Edulytics.Web/Edulytics.Web.csproj \
    --context EdulyticsDbContext \
    --configuration Release \
    --output /app/efbundle \
    --force

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Npgsql / EF migration bundle can require the system Kerberos GSSAPI
# library before ASP.NET binds its HTTP port. The base ASP.NET runtime
# image does not guarantee this native dependency.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=build /app/publish ./
COPY --from=build /app/efbundle /app/efbundle
COPY docker/render-entrypoint.sh /app/render-entrypoint.sh
COPY docker/phase27-predeploy.sh /app/phase27-predeploy.sh

RUN chmod 0555 \
    /app/efbundle \
    /app/render-entrypoint.sh \
    /app/phase27-predeploy.sh

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_EnableDiagnostics=0

EXPOSE 10000

USER app

ENTRYPOINT ["/app/render-entrypoint.sh"]
