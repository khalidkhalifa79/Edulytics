FROM mcr.microsoft.com/dotnet/sdk:10.0.200 AS build

WORKDIR /src

COPY global.json Directory.Build.props ./

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

COPY src/ src/

RUN dotnet publish \
    src/Edulytics.Web/Edulytics.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish ./

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

USER app

ENTRYPOINT ["dotnet", "Edulytics.Web.dll"]
