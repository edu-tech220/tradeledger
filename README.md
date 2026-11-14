# TradeLedger

Personal brokerage order execution and portfolio ledger platform, built as two ASP.NET Core 8 microservices (Web API + Razor Pages) with EF Core, SQL Server and xUnit.

> Work in progress — this README will grow as the solution is built out branch by branch.

## Solution layout

- `src/TradeLedger.Api` — ASP.NET Core Web API (backend microservice)
- `src/TradeLedger.Web` — ASP.NET Core Razor Pages (frontend)
- `tests/TradeLedger.Api.Tests` — xUnit tests for the backend

## Running locally

```bash
dotnet build
dotnet test
dotnet run --project src/TradeLedger.Api
dotnet run --project src/TradeLedger.Web
```
