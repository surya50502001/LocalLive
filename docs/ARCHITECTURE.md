# LocalLive — Hyperlocal LIVE "I Need This Right Now" Network

## Product

Customer creates a **LIVE request**. Nearby relevant, **OPEN** shops receive it in real time.
A shop presses **[AVAILABLE]**. The customer instantly sees the shop + distance and presses **[GO THERE]** to open navigation.

No quotation, no bidding, no negotiation, no checkout, no delivery. `REQUEST → LIVE SHOP RESPONSE → GO TO SHOP`.

---

## 1. Final Architecture (Backend)

Clean/Onion architecture with a maintainable vertical slice. Avoids over-abstraction but separates concerns.

```
backend/
  LocalLive.sln
  src/
    LocalLive.Domain/        # Entities, Enums, Value Objects, Interfaces (no frameworks)
    LocalLive.Application/   # DTOs, Validators (FluentValidation), Services, Interfaces
    LocalLive.Infrastructure/# EF Core DbContext, Repositories, Auth (JWT), SignalR service, Geospatial
    LocalLive.Api/           # Controllers, Hubs, Middleware, Program.cs, DI wiring
  tests/
    LocalLive.Application.Tests/   # Unit tests (services, matching, expiry)
    LocalLive.Api.Tests/           # Integration tests (WebApplicationFactory + real Postgres or testcontainers)
```

Layered dependencies: `Api → Application → Domain`, `Api → Infrastructure → Domain`, `Application → Domain`. Domain references nothing.

## 2. Database ERD

```
User 1 ─── * RefreshToken
User 1 ─── * Shop                 (owner)  [Role SHOP_OWNER]
User 1 ─── * LiveRequest          (customer) [Role CUSTOMER]
User 1 ─── * Report               (reporter)
User 1 ─── * AdminAction          (admin executor)

Category 1 ─── * ShopCategory     ─── * Shop
Shop 1 ─── * ShopCategory
Shop 1 ─── * ShopRequest          ─── * LiveRequest
Shop 1 ─── * ShopResponse         (only shops notified about a request can respond)

LiveRequest 1 ─── * ShopRequest   (per-shop delivery record + per-shop response)
LiveRequest 1 ─── * Notification   (customer notifications)
ShopRequest 1 ─── 0..1 ShopResponse

Shop 1 ─── * Report               (reported shop)
LiveRequest 1 ─── * Report         (reported request)
User 1 ─── * Notification         (recipient)
```

## 3. Database Schema (PostgreSQL)

All tables use `uuid` PKs (with `gen_random_uuid()`), `timestamp with time zone` timestamps, soft delete (`deleted_at`), audit fields (`created_at`, `updated_at`).

- **users**: id, email (unique), phone (nullable), password_hash, full_name, role (customer|shop_owner|admin), is_blocked, blocked_at, is_verified, created_at, updated_at, deleted_at
- **refresh_tokens**: id, user_id (FK), token (unique, hashed), expires_at, created_at, revoked_at, replaced_by_token_id
- **categories**: id, name, slug (unique), icon, sort_order, is_active, created_at, updated_at, deleted_at
- **shops**: id, owner_user_id (FK unique), name, description, phone, address, latitude, longitude (geolocation), is_verified, is_active, is_open, opening_hours (HoursOfOperation JSON), image_url, status (pending|approved|disabled), created_at, updated_at, deleted_at
- **shop_categories**: id, shop_id (FK), category_id (FK), UNIQUE(shop_id, category_id)
- **live_requests**: id, customer_user_id (FK), category_id (FK), title, description, latitude, longitude, status (active|fulfilled|cancelled|expired), expires_at, created_at, updated_at, closed_at, deleted_at + indexes on status, (status, geometry), (customer, status)
- **shop_requests**: id, request_id (FK), shop_id (FK), status (notified|responded|expired|fulfilled|cancelled), notified_at, responded_at, UNIQUE(request_id, shop_id)
- **shop_responses**: id, request_id (FK), shop_id (FK), responder_user_id (FK), status (available), distance_m, message, created_at, UNIQUE(request_id, shop_id) — one response per shop per request
- **notifications**: id, recipient_user_id (FK), type, title, body, payload (jsonb), is_read, read_at, created_at — index on (recipient, is_read)
- **reports**: id, reporter_user_id (FK), target_type (shop|request), target_id (uuid), reason, created_at, status (open|resolved|dismissed), resolved_by_id, resolved_at
- **admin_actions**: id, admin_user_id (FK), target_type, target_id, action, detail (jsonb), created_at

Geo indexes: PostGIS optional; primary search uses `earthdistance`/`cube` or bounding-box filtering with radius computed in app. To keep the DB lightweight but real and production-grade, we enable the **`cube` + `earthdistance`** extensions for accurate radius search, and make the map/navigation provider pluggable.

## 4. API Contract

All JSON. Auth via JWT Bearer. Errors: `{ "type", "title", "status", "detail", "traceId", "errors" }`.

### Auth
- `POST /api/auth/register` — body `{email, password, fullName, phone?}` → `{user, accessToken, refreshToken}` (returns role, and can auto-register as shop owner with `registerAs:` role)
- `POST /api/auth/login` → `{user, accessToken, refreshToken}`
- `POST /api/auth/refresh` — `{refreshToken}` → new tokens
- `POST /api/auth/logout` — revoke refresh token
- `GET /api/auth/me`

### Categories
- `GET /api/categories` — active categories

### Shops (SHOP_OWNER)
- `POST /api/shops` — create shop
- `GET /api/shops/{id}`
- `PUT /api/shops/{id}` — owner only
- `PUT /api/shops/{id}/status` — `{isOpen}` OPEN/CLOSED (owner only)
- `GET /api/shops/me` — my shop
- `GET /api/shops/nearby?latitude&longitude&radiusKm&categoryId` — public, nearby OPEN + verified shops
- `GET /api/shops/{id}/requests/live` — live requests targeted at my shop (owner)

### Requests (CUSTOMER)
- `POST /api/requests` — `{title, description?, categoryId, latitude, longitude, ttlMinutes?}` → created + broadcast to shops
- `GET /api/requests/{id}`
- `POST /api/requests/{id}/cancel` — customer only, while active
- `POST /api/requests/{id}/fulfill` — customer only, while active
- `POST /api/requests/{id}/available` — shop only; `{message?}` — shop responds AVAILABLE; notifies customer via SignalR
- `GET /api/requests/my/live` — customer's active requests

### Notifications
- `GET /api/notifications` — my notifications

### Reports
- `POST /api/reports` — report a shop or request

### Health
- `GET /health` — liveness
- `GET /health/ready` — readiness (checks DB)

### Admin (`/api/admin/*`, role ADMIN)
- `POST /api/admin/auth/login` (or reuse auth login w/ admin role)
- `GET /api/admin/shops` — paged, filter by status
- `POST /api/admin/shops/{id}/verify`
- `POST /api/admin/shops/{id}/disable`
- `POST /api/admin/shops/{id}/enable`
- `GET /api/admin/users`, `POST /api/admin/users/{id}/block`, `.../unblock`
- `GET /api/admin/requests`
- `GET /api/admin/reports`, `POST /api/admin/reports/{id}/resolve`, `.../dismiss`
- `GET /api/admin/stats` — analytics
- `GET/POST/PUT/DELETE /api/admin/categories`

## 5. SignalR Event Architecture

Hub: `live` at `/hubs/live`.

Authentication: JWT Bearer in `access_token` query param (SignalR access token standard).

Connection groups / user identifiers:
- Customers joined to `user:{userId}` (to receive `requestAvailable`, `requestStatusChanged`).
- Shop owners joined to `shop:{shopId}` (to receive `newRequest`) — after a shop is created, the owner is added to their shop group.

Events:
| Direction | Event | Payload |
|---|---|---|
| Server→Shop (owner) | `newRequest` | `{requestId, title, categoryName, distanceM, customerNote, expiresAt, customerLatitude, customerLongitude}` |
| Server→Customer | `shopAvailable` | `{requestId, shopId, shopName, distanceM, verified, open, responseId}` |
| Server→Customer | `requestStatusChanged` | `{requestId, status}` |
| Server→Shop | `requestCancelledOrExpired` | `{requestId}` (cleanup for shop UI) |

Delivery: When a request is created, the backend persists `ShopRequest` rows for matching open/verified shops (delivery record), then pushes `newRequest` to each shop's SignalR group. This satisfies "Record delivery/notification information."

## 6. Authentication Architecture

- **Registration**: password hashed with PBKDF2 (ASP.NET Core `PasswordHasher` v3, iterated/salted).
- **Login**: verifies password, checks `is_blocked`, issues tokens.
- **Access token**: short-lived JWT (15 min), claims: `sub` (user id), `role`, `name`, `email`, `jti`.
- **Refresh token**: random 256-bit, **stored hashed (SHA-256)** in `refresh_tokens`, long-lived (7 days), rotation + reuse detection, revocable on logout.
- **Authorization**: `[Authorize(Roles = "...")]` + custom policies. Shop-scoped checks in services (owner can only modify own shop; customer only own request). Ownership enforced in service layer, not just attributes.
- **Rate limiting**: ASP.NET Core built-in rate limiter policies — per-endpoint (e.g., login/register/refresh fixed window, request creation sliding window, global).
- **CORS**: configured from env; `GET /api/categories`, `GET /api/shops/nearby` public; all others require auth.
- Secrets: all via env vars / user-secrets; `.env` used by compose; nothing hardcoded. `appsettings.json` contains no secrets.

## 7. Location Architecture

- Data: latitude/longitude doubles on shops and live_requests.
- Provider abstraction: `ICoordinatesMapper` / optional reverse-geocoding; navigation link generator `INavigationProvider` interface (Google Maps, Apple Maps, Waze, OpenStreetMap) configurable via env. Not hardcoding/tying to a single provider.
- Radius search: DB via `cube`/`earthdistance` extension for accurate nearby matching; computed `distance_m` returned in API.
- Shop receives distance to customer; customer receives distance to shop.
- Frontend uses browser Geolocation API to get real coordinates (with graceful permission handling).

## 8. Frontend Architecture

```
frontend/
  Vite + React + TypeScript + Tailwind v4 + react-router
  src/
    api/       # typed API client (services per resource) + axios/fetch wrapper with interceptors
    hooks/     # useAuth, useSignalR, useGeolocation, useToast
    lib/       # signalr client, auth store (zustand), utils
    components/# UI primitives (Button, Input, Card, Badge, Spinner, Modal, Toast)
    pages/
      public/  # Landing, Login, Register
      customer/# Home(Live feed), CreateRequest, RequestLive, RequestDetail
      shop/    # Dashboard, Onboarding, Requests
      admin/   # Login, Dashboard(Stats), Shops, Users, Requests, Reports, Categories
    router/    # route guards (auth, role-based)
```

State: Zustand for auth/UI state. Real-time via `@microsoft/signalr`. Responsive via Tailwind (mobile-first).

## 9. Deployment Architecture

- Backend: Dockerfile (multi-stage, `aspnet:8.0` runtime) — published on Alpine/debian-slim.
- Frontend: Dockerfile multi-stage (Node build → Nginx), Nginx serves SPA + proxies `/api` and `/hubs` to backend; configured SPA fallback.
- `docker-compose.yml`: services `db` (postgres:16-alpine with healthcheck + volume), `api`, `web` (nginx). `.env.example` documents all vars.
- Production readiness: health checks (`/health`, `/health/ready`), structured logging (Serilog to console with optional Seq), Swagger/OpenAPI, migration-on-startup (with env flag `APPLY_MIGRATIONS`), CORS env, CSRF-less JWT.
- CI/CD-ready: GitHub Actions workflow yaml (test → build → publish images).

## 10. Implementation Roadmap

1. **Scaffold**: sln + 4 backend projects + 3 test projects; verify build.
2. **Domain**: entities, enums, interfaces.
3. **Infrastructure**: DbContext, configuration, initial migration, seed.
4. **Auth**: JWT + refresh + AuthService + AuthController + tests.
5. **Categories**: read API.
6. **Shops**: CRUD + status + nearby + middleware ownership.
7. **Requests**: create (+ matching + notification recording), get, cancel, fulfill; SignalR broadcast + expiry background service.
8. **Available response**: shop respond → notify customer via SignalR.
9. **SignalR hub + frontend realtime client.**
10. **Frontend scaffold + auth + routing guards.**
11. **Frontend customer flow + live feed + request live page.**
12. **Frontend shop flow + dashboard.**
13. **Frontend admin panel.**
14. **Reports + notifications.**
15. **Tests: unit (matching, expiry, auth, validation) + integration (end-to-end via WebApplicationFactory).**
16. **Docker + compose + CI + docs.**
17. **End-to-end verification of every flow.**

Each feature: backend → migrations → API → frontend → connect → validation/errors → tests → verify.
