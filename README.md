# GitHub API Client

A production-grade .NET 8 proxy API that wraps the GitHub REST API. The project demonstrates a complete HTTP client architecture: multi-mode authentication with token caching, structured request/response logging with full traceability, layered error handling, FluentValidation request validation, and raw JSON passthrough to avoid coupling the proxy to GitHub's response schema.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Project Structure](#project-structure)
- [Endpoints](#endpoints)
- [Authentication](#authentication)
- [Error Handling](#error-handling)
- [Logging](#logging)
- [Validation](#validation)
- [Configuration](#configuration)
- [Tech Stack](#tech-stack)
- [Getting Started](#getting-started)

---

## Architecture Overview

The project follows a strict layered architecture where each component has a single, well-defined responsibility.

**Request flow:**

```
Client → RequestLoggingMiddleware → Controller → Service → GitHub API
                                                         ↓
Client ← RequestLoggingMiddleware ← Controller ← Service ← GitHub API
```

**Key design decisions:**

**Named HttpClients over Typed Clients.** `AuthorizationService` is registered as a Singleton, which makes typed client injection unsafe — typed clients have a scoped lifetime and inject a short-lived `HttpClient` into a long-lived service, causing socket exhaustion over time. Named clients via `IHttpClientFactory` avoid this entirely.

**Services return `HttpResponseMessage`.** The service layer makes the HTTP call and returns the raw response. Controllers own the response processing. This means the service has no awareness of what the controller does with the response — it executes the call and returns the result. Raw JSON is passed directly to the client without deserialization, which eliminates the risk of the proxy silently dropping fields that GitHub adds to their API over time.

**Options pattern throughout.** All configuration is accessed via `IOptions<ExternalApisOptions>`. No raw `IConfiguration` lookups anywhere in the codebase.

**`HandleExternalResponse` as a shared private method.** Every controller action funnels success and error responses through a single private method. Success responses preserve the original HTTP status code from GitHub (200 for GET, 201 for POST). Error responses are parsed and mapped through `ErrorCodeMapper`. This centralises response-handling logic and keeps every action method clean.

---

## Project Structure

```
GitHubClient/
├── Configuration/
│   └── ExternalApisOptions.cs          # Strongly-typed config with AuthMode enum
├── Controllers/
│   └── GitHubController.cs             # Orchestration only — no business logic
├── Helpers/
│   └── ErrorCodeMapper.cs              # GitHub status codes → standardised error responses
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs  # Global unhandled exception → 500
│   └── RequestLoggingMiddleware.cs     # Bookends every request with timing and status
├── Models/
│   ├── Requests/
│   │   ├── GitHubQueryParameters.cs
│   │   ├── GitHubUsernameParameters.cs
│   │   ├── GitHubUserReposQueryParameters.cs
│   │   └── GitHubCreateRepoRequest.cs
│   └── Responses/
│       ├── ErrorResponse.cs
│       ├── ExternalApiErrorResponse.cs
│       ├── GitHubUserResponse.cs
│       ├── GitHubPublicUserResponse.cs
│       ├── GitHubRepositoryResponse.cs
│       ├── GitHubCreateRepoResponse.cs
│       └── TokenResponse.cs
├── Services/
│   ├── IAuthorizationService.cs
│   ├── AuthorizationService.cs
│   ├── IGitHubService.cs
│   └── GitHubService.cs
├── Validators/
│   ├── GitHubQueryParametersValidator.cs
│   ├── GitHubRequestValidator.cs
│   ├── GitHubUsernameParametersValidator.cs
│   ├── GitHubUserReposQueryParametersValidator.cs
│   └── GitHubCreateRepoRequestValidator.cs
├── appsettings.json
├── appsettings.Development.json
└── Program.cs
```

---

## Endpoints

### GET /api/github/me

Retrieves the authenticated user's profile. Maps to `GET /user` on the GitHub API. Requires a valid token with at least read access.

**Response:** `200 OK` — GitHub user object.

---

### GET /api/github/users/{username}

Retrieves a public user profile by username. Maps to `GET /users/{username}`.

**Path parameter:**

| Parameter  | Type   | Required | Validation |
|------------|--------|----------|------------|
| `username` | string | Yes      | Max 39 characters. Alphanumeric and hyphens only. Cannot start or end with a hyphen. |

**Response:** `200 OK` — public user profile object.

---

### GET /api/github/users/{username}/repos

Retrieves repositories for a given user. Maps to `GET /users/{username}/repos`. Supports sorting and pagination. Both the path parameter and query parameters are validated independently — if username validation fails, query validation is not run.

**Path parameter:**

| Parameter  | Type   | Required | Validation |
|------------|--------|----------|------------|
| `username` | string | Yes      | Same rules as above. |

**Query parameters:**

| Parameter  | Type    | Required | Validation |
|------------|---------|----------|------------|
| `sort`     | string  | No       | Must be one of: `created`, `updated`, `pushed`, `full_name`. |
| `per_page` | integer | No       | Between 1 and 100 inclusive. |
| `page`     | integer | No       | Greater than or equal to 1. |

All query parameters are optional. When omitted, GitHub's own defaults apply. Validation only runs when a parameter is present — absent fields are not validated.

**Response:** `200 OK` — JSON array of repository objects. Returns `[]` when the user has no repositories or the requested page is beyond the last page of results.

---

### POST /api/github/repos

Creates a repository for the authenticated user. Maps to `POST /user/repos`. Requires a token with `repo` scope.

**Request body:**

| Field         | Type    | Required | Validation |
|---------------|---------|----------|------------|
| `name`        | string  | Yes      | Max 100 characters. Alphanumeric, hyphens, underscores, and dots. Cannot start with a dot. |
| `description` | string  | No       | Max 350 characters when provided. |
| `private`     | boolean | No       | Defaults to `false`. |
| `auto_init`   | boolean | No       | Defaults to `false`. Initialises the repository with a README when true. |

**Response:** `201 Created` — the full repository object as returned by GitHub.

**Note:** GitHub returns `422 Unprocessable Entity` when a repository with the given name already exists on the account. `ErrorCodeMapper` maps this to `400 Bad Request`.

---

## Authentication

`AuthorizationService` is a thread-safe Singleton that supports three authentication modes. The active mode is driven entirely by configuration — no code changes are required to switch between them.

### Modes

**`StaticToken`**

Returns a token directly from configuration. No HTTP call is made. Suitable for personal access tokens and development use.

```json
"AuthorizationApi": {
  "AuthMode": "StaticToken",
  "StaticToken": "your-token-here"
}
```

**`BearerRefresh`**

Acquires a token by POSTing `{ client_id, client_secret }` as JSON to a token endpoint. The token is cached and reused until it expires, with a 30-second buffer before the actual expiry to account for clock skew and network latency. A `SemaphoreSlim` with a double-checked lock pattern ensures that concurrent requests during a cache miss result in exactly one token fetch rather than a stampede.

```json
"AuthorizationApi": {
  "AuthMode": "BearerRefresh",
  "TokenUrl": "https://auth.example.com/token",
  "ClientId": "your-client-id",
  "ClientSecret": "your-client-secret"
}
```

**`OAuth2`**

Same caching and concurrency behaviour as `BearerRefresh`, but sends credentials as `application/x-www-form-urlencoded` with a `grant_type` field. Suitable for standard OAuth2 client credentials flows.

```json
"AuthorizationApi": {
  "AuthMode": "OAuth2",
  "TokenUrl": "https://auth.example.com/oauth/token",
  "ClientId": "your-client-id",
  "ClientSecret": "your-client-secret",
  "GrantType": "client_credentials"
}
```

### Token caching

For `BearerRefresh` and `OAuth2`, the token is cached in memory on the Singleton instance. The cache is checked before any network call. If the token is still valid, it is returned immediately. The lock is only acquired when the token is absent or within 30 seconds of expiry, at which point a fresh token is fetched, cached, and returned. A double-checked lock inside the semaphore prevents redundant fetches if two requests enter the refresh window simultaneously.

---

## Error Handling

Errors are handled across three distinct layers, each with a specific responsibility.

### Layer 1 — Service (network-level failures)

The service catches exceptions that occur during the HTTP call and logs them before rethrowing. Stack traces are intentionally not logged at this layer.

| Exception              | Meaning           | Logged as |
|------------------------|-------------------|-----------|
| `TaskCanceledException`  | Request timed out | Error, message only |
| `HttpRequestException`   | Network failure   | Error, message only |

### Layer 2 — Controller (exception mapping)

The controller catches rethrown exceptions from the service and maps them to HTTP status codes. The full stack trace is logged here.

| Exception                 | Response         | Status |
|---------------------------|------------------|--------|
| `TaskCanceledException`     | `TIMEOUT_ERROR`  | 504    |
| `InvalidOperationException` | `CONFIGURATION_ERROR` | 500 |
| `HttpRequestException`      | `GATEWAY_ERROR`  | 502    |

`InvalidOperationException` is thrown by `AuthorizationService` when the token is empty or not configured, so it is surfaced as a configuration error rather than a network error.

### Layer 3 — ExceptionHandlingMiddleware (safety net)

Any exception that is not caught by the controller catches is handled here and returned as a generic `500 Internal Server Error`. This layer exists as a safety net and should rarely be triggered in practice.

### Non-exception error paths

**Validation failures** are handled before any service call is made. FluentValidation runs against the request model, and if any rules fail, a `400 Bad Request` is returned immediately. All validation errors across all fields are returned in a single response — not one at a time. No HTTP call is ever made for an invalid request.

**External API errors** (e.g. `401 Unauthorized`, `404 Not Found`, `422 Unprocessable Entity`) do not throw exceptions — the HTTP call succeeds at the network level, and GitHub returns an error status code with a body. These are handled by `HandleExternalResponse` in the controller, which passes the response to `ErrorCodeMapper`. The mapper parses GitHub's `{ "message": "...", "documentation_url": "..." }` error format and maps the external status code to the appropriate proxy response.

| GitHub Status | Proxy Response |
|---------------|----------------|
| 401           | 401            |
| 403           | 403            |
| 404           | 404            |
| 422           | 400            |
| 500           | 500            |
| 503           | 502            |
| Unmapped      | 502            |

### Error response format

All error responses share the same envelope:

```json
{
  "errors": [
    {
      "type": "VALIDATION_ERROR",
      "code": "1900004",
      "message": "Name is required."
    }
  ]
}
```

| Type                 | Code      | Trigger |
|----------------------|-----------|---------|
| `VALIDATION_ERROR`   | `1900004` | FluentValidation failure |
| `TIMEOUT_ERROR`      | `9999998` | Request timed out |
| `CONFIGURATION_ERROR`| `9999997` | Missing or empty token |
| `GATEWAY_ERROR`      | `9999999` | Network failure |
| `EXTERNAL_ERROR`     | HTTP code | Error returned by GitHub |
| `INTERNAL_ERROR`     | `9999999` | Unhandled exception (middleware fallback) |

---

## Logging

Every request is traceable from entry to exit through a `TransactionId` generated at the start of each controller action:

```csharp
var transactionId = $"{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 32);
```

This ID is passed explicitly through every method call — controller to service to authorization service — and appears in every log block. No ambient state or scoped services are used for this.

### Log format

Every log block uses a consistent separator format:

```
********************************************
ClassName :: MethodName :: What happened
Key: Value
TransactionId: {TransactionId}
********************************************
```

### Log flow — happy path (POST)

```
RequestLoggingMiddleware  :: Incoming request
Controller                :: Request received
AuthorizationService      :: Returning static token from configuration
GitHubService             :: Calling external POST — URL + RequestBody
GitHubService             :: httpstatuscode :: "Created" :: SUCCESS :: responseBody
Controller                :: Response received from service :: Processing
Controller                :: HandleExternalResponse :: Returning SUCCESS response
RequestLoggingMiddleware  :: Request completed :: SUCCESS — StatusCode: 201 — Duration: Xms
```

`RequestLoggingMiddleware` bookends every request with timing and a status label: `SUCCESS` (2xx), `REDIRECT` (3xx), `CLIENT ERROR` (4xx), `SERVER ERROR` (5xx).

### Log levels

| Level     | Used for |
|-----------|----------|
| `Information` | Happy path — request received, token acquired, response returned |
| `Warning`     | Validation failures, unmapped external errors |
| `Error`       | Exceptions — service logs message only, controller logs full stack trace |

Stack traces appear exactly once per error, in the controller. The service logs the "what" (message only), the controller logs the "why" (full exception).

### Serilog configuration

Serilog is configured in `Program.cs` and writes to both console and rolling daily log files under `logs/`. Microsoft and `System.Net.Http` noise is suppressed via `MinimumLevel.Override` so that only application-level logs appear in normal operation.

---

## Validation

All validation uses FluentValidation. Validators are registered in DI and injected into the controller constructor. Validation is never done inline.

For GET endpoints with optional query parameters, all rules are wrapped in `When` guards so that absent fields are never validated:

```csharp
When(x => x.PerPage.HasValue, () =>
{
    RuleFor(x => x.PerPage!.Value)
        .InclusiveBetween(1, 100)
        .WithMessage("PerPage must be between 1 and 100.");
});
```

For the POST endpoint, required fields are validated unconditionally and optional fields use `When` guards:

```csharp
RuleFor(x => x.Name)
    .NotEmpty().WithMessage("Name is required.")
    .MaximumLength(100).WithMessage("Name must not exceed 100 characters.")
    .Matches(@"^[a-zA-Z0-9_][a-zA-Z0-9._-]*$")
    .WithMessage("Name must contain only alphanumeric characters, hyphens, underscores, or dots, and cannot start with a dot.");

When(x => !string.IsNullOrEmpty(x.Description), () =>
{
    RuleFor(x => x.Description)
        .MaximumLength(350)
        .WithMessage("Description must not exceed 350 characters.");
});
```

Where multiple fields fail simultaneously, all errors are returned in a single response — the errors array will contain one entry per failing rule.

For endpoints with both path and query parameters, validation runs in sequence: path parameters first, then query parameters. If the path parameter fails, query validation is skipped entirely.

---

## Configuration

All configuration lives under the `ExternalApis` section in `appsettings.json`:

```json
{
  "ExternalApis": {
    "GitHubApi": {
      "BaseUrl": "https://api.github.com",
      "TimeoutSeconds": 30
    },
    "AuthorizationApi": {
      "AuthMode": "StaticToken",
      "StaticToken": "your-github-token",
      "TokenUrl": "",
      "ClientId": "",
      "ClientSecret": "",
      "GrantType": "client_credentials"
    }
  }
}
```

`appsettings.json` should contain placeholder values only and is safe to commit. Real credentials go in `appsettings.Development.json`, which is excluded from source control via `.gitignore`.

To switch authentication mode, change `AuthMode` to `StaticToken`, `BearerRefresh`, or `OAuth2` and populate the corresponding fields. No code changes are required.

---

## Tech Stack

| Package | Version | Purpose |
|---------|---------|---------|
| .NET | 8.0 | Target framework |
| `Serilog.AspNetCore` | 8.0.3 | Structured logging with console and file sinks |
| `FluentValidation.DependencyInjectionExtensions` | 11.11.0 | Request validation |
| `Swashbuckle.AspNetCore` | 6.9.0 | Swagger / OpenAPI documentation |

---

## Getting Started

**Prerequisites:** .NET 8 SDK.

**1. Clone the repository**

```bash
git clone https://github.com/your-username/GitHubClient.git
cd GitHubClient
```

**2. Configure credentials**

Create `appsettings.Development.json` in the project root (this file is excluded from source control):

```json
{
  "ExternalApis": {
    "AuthorizationApi": {
      "AuthMode": "StaticToken",
      "StaticToken": "your-github-personal-access-token"
    }
  }
}
```

Generate a token at [github.com/settings/tokens](https://github.com/settings/tokens). For read-only endpoints a `read:user` scope is sufficient. For `POST /api/github/repos` the token requires the `repo` scope.

**3. Run the project**

```bash
dotnet run
```

Swagger UI is available at `https://localhost:{port}/swagger` when running in the Development environment.

**4. Log output**

Logs are written to the console and to `logs/log-YYYYMMDD.txt`. Each file rolls daily and is retained for 30 days.