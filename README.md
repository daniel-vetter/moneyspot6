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

### SQLite

```bash
docker run -d --restart unless-stopped -p 80:80 -v moneyspot6-data:/app/data dvetter/moneyspot6
```

Data is stored in a SQLite database in `/app/data`.

### PostgreSQL

```bash
docker run -d --restart unless-stopped -p 80:80 -e ConnectionStrings__db="Host=myserver;Database=moneyspot;Username=postgres;Password=secret" dvetter/moneyspot6
```

### Self-Update

MoneySpot6 can update itself from the UI (Settings > System). To enable this, mount the Docker socket:

```bash
docker run -d --restart unless-stopped -p 80:80 -v /var/run/docker.sock:/var/run/docker.sock -v moneyspot6-data:/app/data dvetter/moneyspot6
```

The app checks for new images periodically and lets you apply updates with one click. Update logs are persisted and viewable in the UI.

> **Security note:** Mounting the Docker socket gives the container full control over the Docker daemon on the host. Only do this if you trust the application and run it in a private network. Without the socket mounted, the app works fine but the update feature will be disabled.

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
