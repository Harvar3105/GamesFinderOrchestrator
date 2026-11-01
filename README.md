# GamesFinder Orchestrator

> Second iteration of development of the GamesFinder application

This is the backend service for the GamesFinder project. The main purpose is to orchestrate data collection tasks for workers and deliver aggregated game data to the frontend.

## 📋 Overview

The orchestrator coordinates with multiple workers to scrape and aggregate game information from various sources. All scraping operations comply with the respective services' Terms of Service (ToS) and `robots.txt` policies.

## 🔄 Supported Sources

### Status Overview

| Status | Platform | Coverage | Notes |
|--------|----------|----------|-------|
| ✅ Implemented | **Steam** | Full | Store API + SteamSpy integration |
| ✅ Implemented | **Instant Gaming** | Full | ~200,000 products via web scraping |
| 📅 Planned | **G2A** | - | Coming soon |

---

## 📦 Data Sources

### Steam

| Property | Details |
|----------|---------|
| **Main API** | store.steampowered.com |
| **Game Data Endpoint** | `/api/appdetails?appids=<id>` |
| **Rate Limit** | 200 requests, then 5-minute cooldown |
| **Query Mode** | Single ID for full data, or multiple IDs for single field |
| **Wishlist Endpoint** | `/dynamicstore/userdata` (requires Steam login) |

**Notes:**
- Wishlist feature is planned for future frontend implementation
- Users must be authenticated via Steam browser login for wishlist access

### SteamSpy

| Property | Details |
|----------|---------|
| **API Base** | steamspy.com |
| **Endpoint** | `/api.php?request=appdetails&appid=<id>` |
| **Purpose** | Game tags, genres, review counts |

### Instant Gaming

| Property | Details |
|----------|---------|
| **Platform** | instant-gaming.com |
| **Product Page Example** | `instant-gaming.com/<id>-` |
| **API** | No dedicated API (web scraping) |
| **Rate Limit** | No documented limitations found |
| **Estimated Catalog** | ~200,000 products |

---

## 🛠️ Technology Stack

- **Backend:** .NET (C#)
- **Workers:** Node.js (TypeScript)
- **Message Queue:** RabbitMQ
- **Cache:** Redis
- **Database:** MongoDB
- **Containerization:** Docker

---

## 🚀 Getting Started

### Prerequisites

| Tool | Needed for |
|------|------------|
| Docker + Docker Compose | Container run; RabbitMQ and Redis in local run |
| .NET SDK 10 | Local backend run |
| Node.js 22+ | Local workers run; building workers for the dev compose |
| MongoDB | Always: it is **not** part of compose, reach it via `MongoDb:ConnectionString` |

### Configuration

Configuration files are not stored in git and must be created manually:

- `src/GamesFinder.Orchestrator.API/appsettings.Development.json` / `appsettings.Production.json`: backend settings (sections `MongoDb`, `Security`, `SteamApi`, `Workers`, `RabbitMQ`, `Redis`).
- `.env` in the repository root: used by docker-compose, the workers and the backend container. In environment variables `__` maps to `:`, so `Redis__Host` overrides `Redis:Host` from `appsettings`.

`.env` template (host names match the compose service/container names):

```env
RABBIT_URL=amqp://rabbitmq
RabbitMQ__HostName=rabbitmq
RabbitMQ__Port=5672
RabbitMQ__Username=<user>
RabbitMQ__Password=<password>
RabbitMQ__DefaultQueue=<queue>
RabbitMQ__SteamRequestsQueue=steam-requests
RabbitMQ__SteamResultsQueue=steam-results
RabbitMQ__InstantGamingRequestsQueue=instantGaming-requests
RabbitMQ__InstantGamingResultsQueue=instantGaming-results

Redis__Host=redis
Redis__Port=6379
Redis__Password=<password>

BACKEND_URL=http://backend:8080/api
BACKEND_CHECK_GAME=/Steam/checkGameExists
BACKEND_CHECK_GAME_EXISTS_BY_NAME=/Steam/checkGameExistsByName
BACKEND_CHECK_GAME_EXISTS_BY_STEAM_ID=/Steam/checkGameExistsBySteamId
BACKEND_CHECK_STEAM_OFFER=/Steam/checkGameOfferExists
BACKEND_CHECK_IG_OFFER=/InstantGaming/checkGameOfferExists
BACKEND_GET_GAME_ID=/Steam/getGameIdBySteamId
BACKEND_GET_IG_OFFER_ID=/InstantGaming/getOfferId
BACKEND_GET_STEAM_OFFER_ID=/Steam/getOfferId

# Optional, defaults are shown
MAX__REQUESTS=200
COOLDOWN_MS=300000
BACKEND_TIMEOUT_MS=5000
STEAM_API_TIMEOUT_MS=5000
STEAM_TAGS_AND_GENRES_REQUEST_DELAY_MS=1000
STEAM_TAGS_AND_GENRES_TIMEOUT_MS=3000
```

For the production container also pass the backend settings that are not in `appsettings.Production.json`, e.g. `MongoDb__ConnectionString`, `MongoDb__Database`, `Security__JWTSecret`, `Security__JWTIssuer`, `Security__JWTAudience`.

All compose files use the external network `shared-network`. Create it once:

```bash
docker network create shared-network
```

### Run in containers

**Development** (hot reload: `dotnet watch` for the backend, nodemon for the workers). The worker containers use `workers/dist` and `workers/node_modules` from the host, so build the workers on the host first:

```bash
cd workers
npm ci
npm run build          # or `npx tsc --watch` in a separate terminal to rebuild on change
cd ..

docker compose -f docker-compose.Development.yml up --build
```

**Production** (images are built from sources, no host mounts):

```bash
docker compose -f docker-compose.Production.yml up --build -d
```

Both variants start RabbitMQ, Redis, the Steam worker, 5 InstantGaming worker replicas and the backend.

| Service | Address |
|---------|---------|
| Backend Swagger | <http://localhost:8082/swagger> |
| RabbitMQ management | <http://localhost:15672> |
| RabbitMQ AMQP | localhost:5672 |
| Redis | localhost:6379 |

Workers may fail on the very first start while RabbitMQ is still initializing; run `docker compose ... up` again if that happens.

### Run locally

1. Start the infrastructure (RabbitMQ and Redis) in containers:

   ```bash
   docker compose -f docker-compose.Development.yml up -d rabbitmq redis
   ```

2. Point `appsettings.Development.json` to local services: `RabbitMQ:HostName` and `Redis:Host` = `localhost`, `MongoDb:ConnectionString` = your MongoDB instance.

3. Start the backend:

   ```bash
   dotnet run --project src/GamesFinder.Orchestrator.API --launch-profile http
   ```

   Swagger: <http://localhost:5159/swagger> (`https` profile: <https://localhost:7247/swagger>).

4. Create `.env.local` in the repository root with overrides for running outside Docker:

   ```env
   RABBIT_URL=amqp://localhost
   RabbitMQ__HostName=localhost
   Redis__Host=localhost
   BACKEND_URL=http://localhost:5159/api
   ```

5. Build and start the workers, each in its own terminal (the later `--env-file` overrides the earlier one):

   ```bash
   cd workers
   npm ci
   npm run build

   node --env-file=../.env --env-file=../.env.local dist/steam/steamWorker.js
   node --env-file=../.env --env-file=../.env.local dist/instantGaming/instantGamingWorker.js
   ```

   Start several InstantGaming workers to match `Workers:InstantGamingWorkerCount`.

### Tests

```bash
cd workers
npm test
```

---

## 📝 License & Compliance

All data collection operations adhere to:
- Service Terms of Service (ToS)
- `robots.txt` guidelines
- Responsible scraping practices
