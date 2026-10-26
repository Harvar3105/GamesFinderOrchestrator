# Project Structure

GamesFinder Orchestrator is the backend part of GamesFinder (the frontend lives in a separate repository). It is a monorepo with two parts:

- `src/` — ASP.NET Core (.NET 10) backend: REST API with JWT auth, scrape task publishing, result consumption, MongoDB access.
- `workers/` — TypeScript (Node, ESM) scrapers that execute tasks received from RabbitMQ.

Currently the system is operated through Swagger, which is also the interface used by the frontend.

## Data flow

```
Swagger / frontend ──HTTP──► Controller ──► *Service (ApplicationServices)
                                              │ splits IDs into batches, creates ScrapeTask
                                              ▼
                             PublisherFactory → WorkerPublisher → RabbitMqPublisher
                                              │ queue *-requests (task JSON)
                                              ▼
                             Worker (TS) ── scrapes ──► Redis: RPUSH <RedisResultKey>[...]
                                              │ queue *-results: { taskId, redisResultKey }
                                              ▼
                             Consumer (BackgroundService) ── reads Redis ──► MongoDB, deletes key, ack
```

Key points:
- Result queues carry only a **notification**; the payload is stored in Redis.
- Workers call the backend over HTTP while scraping (`workers/src/backendUtils.ts`) to check whether a game or offer exists and to fetch its Id, so the backend must be running.
- `TaskRedisKeyPrefix` in the services is a property that generates a **new GUID on every access**, so every published batch gets its own Redis key.

## Backend (`src/GamesFinder.Orchestrator.*`)

Project references: `API → all other projects`; `Services → Domain, Publisher`; `Consumers → Domain, Publisher`; `Repositories → Domain`; `Utils → Domain`. `Domain` depends only on `MongoDB.Bson`.

| Project | Contents |
|---|---|
| **API** | `Program.cs` — DI, JWT, CORS, Swagger, consumers registered as hosted services. Controllers plus request contracts in `Controllers/Contracts/{Steam,InstantGaming}`. |
| **Domain** | Entities (`Entity`, `Game`, `GameOffer`, `UserData`), tasks (`ScrapeTask`, `SteamScrapeTask`, `InstantGamingScrapeTask`), enums (`ECurrency`, `EVendor`, `ESort`, `EPriceCompare`), `PaginationFilterDto`, options (`SteamOptions`, `WorkersOptions`), all interfaces. |
| **Publisher** | `PublisherFactory` (maps task type → queue), `WorkerPublisher<T>`, `RabbitMQ/` (config, publisher), `Redis/` (`RedisConfig`, `RedisCacheDB` — Newtonsoft.Json wrapper). |
| **Consumers** | `Consumer<TResult>` base class: consumes a results queue, loads items from Redis, calls `SaveToDatabaseAsync`, deletes the key, acks (nack with requeue on error). `SteamWorkerConsumer` reads `<key>:games` and `<key>:offers`; `InstantGamingWorkersConsumer` reads `<key>` (list of `GameOffer`). |
| **Repositories** | `Repository<T>` — generic CRUD over `IMongoCollection`. Collections: `games`, `game_offers`, `users_data`. `MongoDBSettings`. |
| **Services** | `ApplicationServices/` — `VendorsService<T>` (base), `SteamService`, `InstantGamingService`: task publishing. `DomainServices/` — `GamesWithOffersService` (games joined with offers, paging and filters), `Service<T,R>` (generic repository wrapper, currently unused). |
| **Utils** | `Annotations/MinimumInstantGamingId` — validation attribute (currently checks only that the value is an int). |

### Endpoints (`api/[controller]`)

- **Steam**: `POST scrap` (role `admin_gfp`); `GET checkGameExistsByName`, `checkGameExists`, `getGameIdBySteamId`, `checkGameOfferExists`, `getOfferId`, `getPagedWithCurrency`, `getPaged`.
- **InstantGaming**: `POST scrapIds`, `scrapRange`, `scrapUpTo` (role `admin_gfp`); `GET checkGameOfferExists`, `getOfferId`.
- **UserData**: `GET userData`, `PUT saveOrUpdate` (`[Authorize]`, user Id taken from the `sub` claim).

GET endpoints without `[Authorize]` are used by the workers. With `Security:RequireJWT=true` a fallback policy requires authentication for them too, and the workers do not send a token.

### Data model

- `Game` — Steam-centric record (`steam_id`, SteamSpy metadata in `store_metadata`). `Offers` is `[BsonIgnore]` and is loaded from `game_offers` by `game_id`.
- `GameOffer` — a single vendor's offer: `vendor`, `vendors_game_id`, `amount`, `currency`, `available`.
- Ids are `Guid`s stored as strings in MongoDB; `ECurrency` and `EVendor` are stored as strings.
- Workers generate Ids (uuid v4) or reuse existing ones obtained from the backend; consumers upsert by Id.

### Configuration

`appsettings.{Development,Production}.json` (not in git) and `.env` in the repository root (not in git; used by docker-compose, the workers and the Dockerfile). Sections: `MongoDb`, `Security` (JWT*, RequireJWT, CORS), `SteamApi`, `Workers`, `RabbitMQ`, `Redis`. In environment variables `__` maps to `:`.

## Workers (`workers/src`)

Each worker is a separate process: `npm run steam` / `npm run instantgaming` (in dev `*:watch` via nodemon over `dist/`). Build with `npm run build` (tsc → `dist/`). Channels use `prefetch(1)`.

- `utils/config.ts` — env config, Redis and RabbitMQ connections (top-level await).
- `utils/orchestratorListener.ts` — queue assertion and consumption.
- `utils/taskParser.ts` + `utils/types/entities/tasks.ts` — task normalization; accepts both PascalCase (from .NET) and camelCase.
- `utils/offerFetcher.ts` — `fetchJson` (returns `HttpStatusError` instead of throwing), `fetchHTML`, `parseHtmlToDocument` (jsdom).
- `backendUtils.ts` — HTTP helpers for backend lookups.
- `utils/types/` — TypeScript mirrors of domain entities and type guards (`isGame`, `isGameOffer`).

### Steam (`steam/`)

- `steamWorker.ts` — splits a task into batches of `maxRequests` (200); writes results to `<key>:games` and `<key>:offers`.
- `processSteamWorkerTask.ts` — processes IDs, then retries failed ones once; on HTTP 429 waits `cooldownMs` (5 min) and retries.
- `gameFetcher.ts` — `store.steampowered.com/api/appdetails`. Returns a `Game` (with a nested offer) when the game must be created or updated, otherwise only a `GameOffer`.
- `gameMetadataFetcher.ts` — SteamSpy (tags, genres, reviews). **At most one request per second; proxies are blocked.**
- Steam Store API limit: 200 requests, then a 5-minute cooldown.

### InstantGaming (`instantGaming/`)

- No public API: HTML pages `instant-gaming.com/<id>-/?currency=…` are scraped (allowed by robots.txt). The catalog is about 200k IDs.
- Products whose title does not contain "pc" are skipped (`checkIfPCGameIG`).
- Linking an offer to a game: first a Steam ID is extracted from media URLs (`apps/<id>/extras`); otherwise the game is looked up **by title** (text before `-`) via `checkGameExistsByName`. Title matching is unreliable.
- Price comes from `.total`, currency from the price symbol, out-of-stock from `.noStock`.
- docker-compose runs 5 replicas. Batch size is `ceil(count / Workers:InstantGamingWorkerCount)` or `Workers:InstantGamingOverrideBatchSize`.

## Infrastructure

- Dev: `docker-compose.Development.yml` starts rabbitmq, redis, steam-worker, instantgaming-worker (×5) and gamesfinder-api (`dotnet watch`, port 8081→8080). The `shared-network` network is external.
- **MongoDB is not part of compose**; it is reached via `MongoDb:ConnectionString`.
- RabbitMQ queues: `steam-requests`, `steam-results`, `instantGaming-requests`, `instantGaming-results` (`definitions.json`, not in git).
- There are no tests in the repository.

## Conventions

- C#: 2-space indentation (tabs in some files), file-scoped namespaces, emoji log prefixes (`💥`, `⚠️`, `✅`).
- Branches: work happens in `dev`; pull requests target `main`.
