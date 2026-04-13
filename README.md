# MoneySpot6

A self-hosted personal finance management application with German bank integration (FinTS/HBCI), stock tracking, smart categorization, and financial simulations.

## Features

- **Bank Sync** - Connect to German bank accounts via FinTS/HBCI protocol with TAN support
- **Transaction Management** - View, search, override and categorize transactions
- **Hierarchical Categories** - Organize transactions in a tree structure
- **Rules Engine** - Auto-categorize transactions with custom TypeScript rules
- **Stock Tracking** - Monitor portfolio performance with historical price charts
- **Cash Flow Analysis** - Visualize income and spending patterns over time
- **Financial Simulations** - Run forecasting scenarios with a scripting environment
- **Inflation Data** - Track purchasing power with VPI/CPI data (German Federal Statistics Office)
- **Email Monitoring** - Detect transactions from email notifications (Gmail)

## How to Run

```bash
docker run -d -p 80:80 -v moneyspot6-data:/app/data dvetter/moneyspot6
```

The application will be available at `http://localhost`.

By default, the application uses SQLite and stores data in `/app/data`. To use PostgreSQL instead, set the `ConnectionStrings__db` environment variable:

```bash
docker run -d -p 80:80 \
  -e ConnectionStrings__db="Host=myserver;Database=moneyspot;Username=postgres;Password=secret" \
  dvetter/moneyspot6
```

### Image Tags

- `dev` — latest build from the develop branch
- `latest` — latest stable release from master

## Development

### Tech Stack

| Component | Technology |
|---|---|
| Backend | .NET 10, ASP.NET Core, Entity Framework Core |
| Frontend | Angular 20, PrimeNG, Highcharts |
| Bank Adapter | Kotlin/Java 21, HBCI4J |
| Database | SQLite or PostgreSQL |
| Auth | OpenID Connect |
| Local Dev | .NET Aspire |

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Node.js 22](https://nodejs.org/)
- [JDK 21](https://aws.amazon.com/corretto/)
- [Docker](https://www.docker.com/)

### Local Development (Aspire)

```bash
cd src/backend
dotnet run --project MoneySpot6.AppHost
```

This starts all components including PostgreSQL, the HBCI adapter, backend and frontend. Docker must be running.

## Project Structure

```
src/
  backend/
    MoneySpot6.WebApp/          # ASP.NET Core backend + API
    MoneySpot6.WebApp.Tests/    # Unit + Playwright UI tests
    MoneySpot6.AppHost/         # Aspire orchestration
  frontend/                     # Angular SPA
  hbci-adapter/                 # Kotlin HBCI/FinTS bridge
  deployment/                   # Linux systemd install script
```

## License

TBD
