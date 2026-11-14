FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["TradeLedger.sln", "global.json", "./"]
COPY ["src/TradeLedger.Api/TradeLedger.Api.csproj", "src/TradeLedger.Api/"]
COPY ["src/TradeLedger.Web/TradeLedger.Web.csproj", "src/TradeLedger.Web/"]
COPY ["tests/TradeLedger.Api.Tests/TradeLedger.Api.Tests.csproj", "tests/TradeLedger.Api.Tests/"]
RUN dotnet restore "src/TradeLedger.Web/TradeLedger.Web.csproj"

COPY . .
WORKDIR /src/src/TradeLedger.Web
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TradeLedger.Web.dll"]
