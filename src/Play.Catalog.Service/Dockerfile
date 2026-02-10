FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

ENV ASPNETCORE_URLS=http://+:5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG configuration=Release
WORKDIR /src

# Copy nuget.config and packages folder
COPY nuget.config ./
COPY packages ./packages

COPY ["Play.Catalog/src/Play.Catalog.Service/Play.Catalog.Service.csproj", "Play.Catalog/src/Play.Catalog.Service/"]
COPY ["Play.Catalog/src/Play.Catalog.Contracts/Play.Catalog.Contracts.csproj", "Play.Catalog/src/Play.Catalog.Contracts/"]
RUN dotnet restore "Play.Catalog/src/Play.Catalog.Service/Play.Catalog.Service.csproj"
COPY . .
WORKDIR "/src/Play.Catalog/src/Play.Catalog.Service"
RUN dotnet build "Play.Catalog.Service.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "Play.Catalog.Service.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Play.Catalog.Service.dll"]
