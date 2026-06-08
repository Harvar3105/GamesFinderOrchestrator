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

## 📝 License & Compliance

All data collection operations adhere to:
- Service Terms of Service (ToS)
- `robots.txt` guidelines
- Responsible scraping practices
