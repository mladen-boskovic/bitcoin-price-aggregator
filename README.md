# Bitcoin Price Aggregator

A full-stack .NET micro-service that aggregates BTC/USD prices from multiple external sources, persists them, and serves them via a RESTful API consumed by a Blazor WebAssembly frontend.

---

## Architecture

The solution is organised as a multi-project solution following Clean Architecture:

| Project | Role |
|---|---|
| `Domain` | Core entities (`Price`) |
| `Application` | Interfaces, DTOs, CQRS query definitions |
| `Implementation` | Use-case handlers, price providers, repository |
| `DataAccess` | EF Core `AppDbContext`, migrations, SQLite |
| `API` | ASP.NET Core Web API, controllers, middleware |
| `BlazorFrontend` | Blazor WebAssembly client |
| `Tests` | xUnit unit tests for handlers, strategies, and cache keys |

---

## Frontend Technology

**Blazor WebAssembly** (part of ASP.NET Core).

The frontend runs entirely in the browser. It communicates with the backend directly via **HTTP/REST** using .NET's built-in `HttpClient`.

---

## How the Frontend Communicates with the Backend

`PriceApiService` (in `BlazorFrontend/Services/`) wraps all API calls. The `HttpClient` base address is configured at startup to point to the API:

```
https://localhost:7050
```

CORS is configured on the API to allow requests from `https://localhost:7063` (the Blazor dev server).

---

## Prerequisites

- [.NET 10 SDK] (https://dotnet.microsoft.com/download)
- No database server required — SQLite is used and the database file is created automatically.

---

## Running the Application

Configure **Multiple Startup Projects** in Visual Studio:

1. Right-click the solution in Solution Explorer → **Properties**
2. Under **Startup Project**, select **Multiple startup projects**
3. Set the **Action** for both `API` and `BlazorFrontend` to **Start**
4. Click **Apply**, then **OK**
5. Click the **Start** button or press **F5**

The API will be available at `https://localhost:7050` (Swagger UI at `/swagger`).  
The frontend will open automatically at `https://localhost:7063`.

---

## API Endpoints

> The first endpoint is accessible via the Blazor UI. Both endpoints are available through Swagger at `https://localhost:7050/swagger`.

### Get aggregated price at a specific time

```
GET /api/price?instrument=BTCUSD&timestampUtc=2023-01-01T00:00:00Z
```

- Returns the aggregated price for the given instrument and hour-truncated UTC timestamp.
- Lookup follows a three-tier cache strategy:
  - `MemoryCache` — price was served from in-memory cache (fastest)
  - `DbCache` — price was served from the database
  - `ExternalAPIs` — price was fetched live from Bitstamp and/or Bitfinex, then persisted

**Example response**

```json
{
  "instrument": "BTCUSD",
  "timestampUtc": "2023-01-01T00:00:00Z",
  "aggregatedPrice": 16530.25,
  "providerPrices": {
    "Bitstamp": 16531.00,
    "Bitfinex": 16529.50
  },
  "source": "ExternalAPIs"
}
```

### Get persisted price history for a time range

```
GET /api/price/history?from=2023-01-01T00:00:00Z&to=2023-01-02T00:00:00Z
```

Returns all persisted aggregated prices within the specified UTC time range.

---

## Price Sources

| Source | API used |
|---|---|
| **Bitstamp** | `https://www.bitstamp.net/api/v2/ohlc/{instrument}/?step=3600&limit=1&start={unix}` |
| **Bitfinex** | `https://api-pub.bitfinex.com/v2/candles/trade:1h:t{INSTRUMENT}/hist?start={ms}&end={ms}&limit=1` |

**Aggregation formula:** simple average of all available provider prices. If one provider is unavailable, the remaining provider's price is used as the aggregated value.

---

## Assumptions

- Time-points are always truncated to the nearest hour (minutes and seconds are ignored).
- Only `BTCUSD` is supported as an instrument. Adding further instruments requires extending `SupportedInstruments.All` in the `Application` project.
- All prices are stored and returned as `double` (64-bit floating point).
- No authentication or rate-limiting is implemented; the service is intended for local/private use.
- The database file (`prices.db`) is stored in the `API` project working directory.