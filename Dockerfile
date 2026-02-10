# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

ENV ASPNETCORE_URLS=http://+:5000

# Build image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG configuration=Release
ARG GITHUB_TOKEN
ENV NUGET_AUTH_TOKEN=$GITHUB_TOKEN

WORKDIR /src

# Copy nuget.config (for GitHub Packages)
COPY nuget.config ./

# Copy csproj files
COPY src/Play.Catalog.Service/Play.Catalog.Service.csproj src/Play.Catalog.Service/
COPY src/Play.Catalog.Contracts/Play.Catalog.Contracts.csproj src/Play.Catalog.Contracts/

# Restore
RUN dotnet restore src/Play.Catalog.Service/Play.Catalog.Service.csproj

# Copy the rest of the source
COPY src/Play.Catalog.Service/ src/Play.Catalog.Service/
COPY src/Play.Catalog.Contracts/ src/Play.Catalog.Contracts/

# Build
WORKDIR /src/src/Play.Catalog.Service
RUN dotnet build "Play.Catalog.Service.csproj" -c $configuration -o /app/build

# Publish
FROM build AS publish
ARG configuration=Release
RUN dotnet publish "Play.Catalog.Service.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Play.Catalog.Service.dll"]