# ICMarkets Blockchain API

A production-ready ASP.NET Core 10.0 Web API for fetching and storing blockchain data from the BlockCypher API. Built with Clean Architecture, CQRS pattern, and comprehensive testing.

[![.NET 10.0](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED)](https://www.docker.com/)

## 📋 Table of Contents

- [Overview](#-overview)
- [Features](#-features)
- [Architecture](#-architecture)
- [Prerequisites](#-prerequisites)
- [Getting Started](#-getting-started)
  - [Local Development](#local-development)
  - [Docker Deployment](#docker-deployment)
- [API Documentation](#-api-documentation)
- [Configuration](#️-configuration)
- [Testing](#-testing)
- [Project Structure](#-project-structure)
- [Future Enhancements](#-future-enhancements)
- [Contributing](#-contributing)
- [License](#-license)

---

## 🚀 Overview

ICMarkets Blockchain API is a RESTful service that provides:
- Real-time blockchain data fetching from BlockCypher API
- Historical blockchain data storage using SQLite
- Support for multiple blockchains: Bitcoin (BTC), Ethereum (ETH), Litecoin (LTC), Dash (DASH), Dogecoin (DOGE)
- Production-ready features: rate limiting, response caching, compression, health checks, and structured logging

**Tech Stack:**
- **.NET 10.0** - Latest .NET framework
- **ASP.NET Core Web API** - RESTful API framework
- **Entity Framework Core 10.0** - ORM with SQLite database
- **MediatR 12.4** - CQRS and mediator pattern implementation
- **AutoMapper 13.0** - Object-to-object mapping
- **FluentValidation 11.3** - Input validation
- **Serilog 8.0** - Structured logging
- **Polly 8.5** - Resilience and transient-fault-handling
- **xUnit, FluentAssertions, Moq** - Testing framework

---

## ✨ Features

### Core Functionality
- ✅ **Fetch blockchain data** from BlockCypher API (BTC, ETH, LTC, DASH, DOGE)
- ✅ **Store historical data** with timestamps in SQLite database
- ✅ **Query stored data** by chain, network, and time
- ✅ **Batch operations** - fetch all blockchains in parallel

### Production Features
- 🔒 **Rate Limiting** - Fixed window rate limiting (100 req/min general, 10 req/min for fetch)
- ⚡ **Response Caching** - In-memory caching for faster responses
- 🗜️ **Compression** - Gzip and Brotli compression for reduced bandwidth
- 🏥 **Health Checks** - Database and external API health monitoring
- 📊 **Structured Logging** - Serilog with console and file sinks
- 🔄 **Retry Logic** - Polly for resilient HTTP calls with exponential backoff
- 🌐 **CORS** - Configurable cross-origin resource sharing
- 📝 **OpenAPI/Swagger** - Scalar API documentation

### Architecture
- 🏛️ **Clean Architecture** - Domain, Application, Infrastructure layers
- 📬 **CQRS Pattern** - Command Query Responsibility Segregation with MediatR
- ✔️ **Input Validation** - FluentValidation pipeline behavior
- 🧪 **Comprehensive Testing** - Unit, Integration, and Functional tests
- 🐳 **Docker Ready** - Multi-stage Dockerfile and docker-compose configuration

---

## 🏗️ Architecture

The project follows Clean Architecture principles with clear separation of concerns:

```
ICMarkets/
├── Domain/                    # Core business entities and interfaces
│   ├── Entities/             # Domain entities (BlockchainData)
│   └── Interfaces/           # Repository and service interfaces
├── Application/               # Business logic and use cases
│   ├── Commands/             # Write operations (CQRS)
│   ├── Queries/              # Read operations (CQRS)
│   ├── Handlers/             # MediatR command/query handlers
│   ├── DTOs/                 # Data transfer objects
│   ├── Validators/           # FluentValidation validators
│   ├── Behaviors/            # MediatR pipeline behaviors
│   └── Mappings/             # AutoMapper profiles
├── Infrastructure/            # External concerns and data access
│   ├── Data/                 # EF Core DbContext
│   ├── Repositories/         # Repository implementations
│   └── ExternalServices/     # BlockCypher API client
├── Controllers/               # API endpoints
├── Extensions/                # Service registration extensions
└── Constants/                 # Application constants
```

### Design Patterns Used
- **Repository Pattern** - Data access abstraction
- **Unit of Work Pattern** - Transaction management
- **CQRS** - Separation of read and write operations
- **Mediator Pattern** - Decoupled request handling
- **Retry Pattern** - Resilient external API calls
- **Factory Pattern** - Test infrastructure setup

---

## 📦 Prerequisites

### For Local Development
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- [SQLite](https://www.sqlite.org/) (included with EF Core)
- Optional: [Postman](https://www.postman.com/) or [Thunder Client](https://www.thunderclient.com/) for API testing

### For Docker Deployment
- [Docker Desktop](https://www.docker.com/products/docker-desktop) 20.10+ or Docker Engine
- [Docker Compose](https://docs.docker.com/compose/) v2.0+ (included with Docker Desktop)

---

## 🚀 Getting Started

### Local Development

#### 1. Clone the Repository
```bash
git clone https://github.com/TomoKulusic/ICMarkets.git
cd ICMarkets
```

#### 2. Configure Application Settings

The API uses `appsettings.json` for configuration. By default, it's pre-configured and ready to run.

**Optional: Add BlockCypher API Token** (for higher rate limits)
```json
{
  "BlockCypher": {
    "ApiToken": "your-api-token-here"
  }
}
```

Get a free token at [BlockCypher](https://www.blockcypher.com/). Without a token, you'll use the free tier (3 req/sec).

#### 3. Restore Dependencies
```bash
cd ICMarkets
dotnet restore
```

#### 4. Run the Application
```bash
dotnet run
```

The API will start at:
- **HTTP**: `http://localhost:5000`
- **HTTPS**: `https://localhost:5001`

#### 5. Access API Documentation
Open your browser and navigate to:
- **Scalar API Docs**: `https://localhost:5001/scalar/v1`
- **OpenAPI JSON**: `https://localhost:5001/openapi/v1.json`

#### 6. Test the API

**Check Health:**
```bash
curl http://localhost:5000/health
```

**Fetch Blockchain Data:**
```bash
curl -X POST "http://localhost:5000/api/blockchain/fetch?chain=btc&network=main"
```

**Get Latest Stored Data:**
```bash
curl http://localhost:5000/api/blockchain/btc/latest
```

**Get All Stored Data:**
```bash
curl http://localhost:5000/api/blockchain
```

---

### Docker Deployment

**Build the Image:**
```bash
docker build -t icmarkets-api:latest .
```

**Run with Docker Compose:**
```bash
docker-compose up -d
```

**View Logs:**
```bash
docker-compose logs -f
```

**Stop the Container:**
```bash
docker-compose down
```

**Remove Volumes (clean database):**
```bash
docker-compose down -v
```

#### Docker Configuration

The API runs on:
- **Host Port**: `5000`
- **Container Port**: `8080`
- **Access**: `http://localhost:5000`

**Environment Variables:**
```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - ASPNETCORE_URLS=http://+:8080
  - ConnectionStrings__DefaultConnection=Data Source=/app/data/icmarkets.db
```

**Persistent Volumes:**
- `icmarkets-data`: Database storage (`/app/data`)
- `icmarkets-logs`: Application logs (`/app/logs`)

**Health Check:**
- Endpoint: `http://localhost:8080/health`
- Interval: 30s
- Timeout: 10s
- Start period: 40s

---

## 📚 API Documentation

### Endpoints Overview

| Method | Endpoint | Description | Rate Limit |
|--------|----------|-------------|------------|
| GET | `/health` | Health check | None |
| GET | `/api/blockchain` | Get all stored data | 100/min |
| GET | `/api/blockchain/{chain}` | Get data by chain | 100/min |
| GET | `/api/blockchain/{chain}/latest` | Get latest data | 100/min |
| POST | `/api/blockchain/fetch` | Fetch single chain | 10/min |
| POST | `/api/blockchain/fetch-all` | Fetch all chains | 10/min |

### Supported Blockchains

| Chain | Network | Description |
|-------|---------|-------------|
| `btc` | `main`, `test3` | Bitcoin |
| `eth` | `main` | Ethereum |
| `ltc` | `main` | Litecoin |
| `dash` | `main` | Dash |
| `doge` | `main` | Dogecoin |

### Detailed Endpoints

#### 1. Health Check
```http
GET /health
```

**Response:**
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "service": "ICMarkets Blockchain API"
}
```

#### 2. Get All Stored Data
```http
GET /api/blockchain
```

**Response:** Array of blockchain data entries
**Cache Duration:** 30 seconds

#### 3. Get Data by Chain
```http
GET /api/blockchain/{chain}?network={network}
```

**Parameters:**
- `chain` (required): Blockchain identifier (btc, eth, ltc, dash, doge)
- `network` (optional): Network filter (main, test3)

**Example:**
```bash
curl http://localhost:5000/api/blockchain/btc?network=main
```

**Response:**
```json
[
  {
    "id": 1,
    "chain": "btc",
    "network": "main",
    "rawJsonData": "{\"name\":\"BTC.main\",\"height\":820000,...}",
    "createdAt": "2024-01-15T10:00:00Z"
  }
]
```

#### 4. Get Latest Data
```http
GET /api/blockchain/{chain}/latest?network={network}
```

**Example:**
```bash
curl http://localhost:5000/api/blockchain/eth/latest
```

**Cache Duration:** 60 seconds

#### 5. Fetch Fresh Data
```http
POST /api/blockchain/fetch?chain={chain}&network={network}
```

**Parameters:**
- `chain` (required): Blockchain identifier
- `network` (required): Network identifier

**Example:**
```bash
curl -X POST "http://localhost:5000/api/blockchain/fetch?chain=btc&network=main"
```

**Rate Limit:** 10 requests per minute

**Response:** Newly fetched blockchain data

#### 6. Fetch All Blockchains
```http
POST /api/blockchain/fetch-all
```

Fetches data for all supported blockchains in parallel:
- eth/main
- dash/main
- btc/main
- btc/test3
- ltc/main

**Example:**
```bash
curl -X POST http://localhost:5000/api/blockchain/fetch-all
```

**Rate Limit:** 10 requests per minute

**Response:** Array of all fetched blockchain data

### Error Responses

**400 Bad Request:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Chain": ["Chain is required and must be valid"]
  }
}
```

**404 Not Found:**
```json
"No stored data found for chain: btc"
```

**429 Too Many Requests:**
```json
{
  "error": "Rate limit exceeded. Please try again later."
}
```

**500 Internal Server Error:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "An error occurred while processing your request",
  "status": 500,
  "traceId": "0HMVFE5..."
}
```

---

## ⚙️ Configuration

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=icmarkets.db"
  },
  "BlockCypher": {
    "ApiToken": ""  // Optional: Add token for higher rate limits
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000"  // Add your frontend URLs
    ]
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

### Environment Variables (Docker)

```bash
# Required
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
ConnectionStrings__DefaultConnection=Data Source=/app/data/icmarkets.db

# Optional
BlockCypher__ApiToken=your-token-here
Cors__AllowedOrigins__0=http://localhost:3000
```

### Rate Limiting Configuration

**General API Calls:**
- Limit: 100 requests per minute
- Queue: 10 requests
- Status: 429 Too Many Requests

**Fetch Operations:**
- Limit: 10 requests per minute
- Queue: 2 requests
- Status: 429 Too Many Requests

### Caching Configuration

**Response Cache Durations:**
- `/api/blockchain`: 30 seconds
- `/api/blockchain/{chain}`: 30 seconds
- `/api/blockchain/{chain}/latest`: 60 seconds

---

## 🧪 Testing

The project includes comprehensive test coverage across three levels:

### Test Projects

1. **ICMarkets.UnitTests** - Unit tests for business logic
2. **ICMarkets.IntegrationTests** - Integration tests for data access
3. **ICMarkets.FunctionalTests** - End-to-end API tests

### Running Tests

**Run All Tests:**
```bash
dotnet test
```

**Run Specific Test Project:**
```bash
dotet test ICMarkets.UnitTests
dotnet test ICMarkets.IntegrationTests
dotnet test ICMarkets.FunctionalTests
```

**Run with Code Coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
```

**Run with Verbose Output:**
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Test Coverage

- ✅ Command and Query Handlers
- ✅ Input Validation (FluentValidation)
- ✅ Repository Operations
- ✅ Unit of Work Pattern
- ✅ External API Client with Retry Logic
- ✅ Controller Endpoints
- ✅ Rate Limiting
- ✅ Response Caching
- ✅ Complete Workflows

### Example Test Scenarios

**Unit Tests:**
- Validator logic for blockchain commands
- Mapping profiles (AutoMapper)
- Domain entity behavior

**Integration Tests:**
- Repository CRUD operations
- Database queries and indexing
- Unit of Work transactions
- Case-insensitive queries

**Functional Tests:**
- GET/POST endpoint workflows
- Rate limiting enforcement
- Caching behavior
- Error responses (400, 404, 429, 500)
- Complete fetch and retrieve workflows

---

## 📁 Project Structure

```
ICMarkets/
├── ICMarkets/                           # Main API Project
│   ├── Application/
│   │   ├── Behaviors/                   # MediatR pipeline behaviors
│   │   │   └── ValidationBehavior.cs   # FluentValidation integration
│   │   ├── Commands/                    # CQRS write operations
│   │   │   ├── FetchBlockchainDataCommand.cs
│   │   │   └── FetchAllBlockchainsCommand.cs
│   │   ├── Queries/                     # CQRS read operations
│   │   │   ├── GetAllBlockchainDataQuery.cs
│   │   │   ├── GetBlockchainDataByChainQuery.cs
│   │   │   └── GetLatestBlockchainDataQuery.cs
│   │   ├── Handlers/
│   │   │   ├── Commands/                # Command handlers
│   │   │   └── Queries/                 # Query handlers
│   │   ├── DTOs/                        # Data transfer objects
│   │   │   ├── BlockchainDataDto.cs
│   │   │   └── BlockCypherResponse.cs
│   │   ├── Validators/                  # FluentValidation validators
│   │   │   └── FetchBlockchainDataCommandValidator.cs
│   │   └── Mappings/                    # AutoMapper profiles
│   │       └── MappingProfile.cs
│   ├── Controllers/                     # API Controllers
│   │   ├── BlockchainController.cs
│   │   └── HealthController.cs
│   ├── Domain/
│   │   ├── Entities/                    # Domain entities
│   │   │   └── BlockchainData.cs
│   │   └── Interfaces/                  # Domain interfaces
│   │       ├── IBlockchainRepository.cs
│   │       ├── IBlockCypherClient.cs
│   │       └── IUnitOfWork.cs
│   ├── Infrastructure/
│   │   ├── Data/                        # EF Core DbContext
│   │   │   └── ApplicationDbContext.cs
│   │   ├── Repositories/                # Repository implementations
│   │   │   ├── BlockchainRepository.cs
│   │   │   └── UnitOfWork.cs
│   │   └── ExternalServices/            # External API clients
│   │       └── BlockCypherClient.cs
│   ├── Extensions/                      # Service extensions
│   │   └── ServiceCollectionExtensions.cs
│   ├── Constants/                       # Application constants
│   │   └── PolicyConstants.cs
│   ├── Program.cs                       # Application entry point
│   ├── appsettings.json                # Configuration
│   ├── appsettings.Development.json
│   └── appsettings.Production.json
├── ICMarkets.UnitTests/                 # Unit test project
├── ICMarkets.IntegrationTests/          # Integration test project
├── ICMarkets.FunctionalTests/           # Functional test project
│   └── TestWebApplicationFactory.cs    # Test server setup
├── Dockerfile                           # Multi-stage Docker build
├── docker-compose.yml                   # Docker Compose configuration
├── .dockerignore                        # Docker ignore patterns
├── .gitignore                          # Git ignore patterns
├── .editorconfig                       # Code style configuration
└── README.md                            # This file
```

---

## 🔮 Future Enhancements

### Microservices Architecture

Transform the monolithic application into a scalable microservices architecture:

```
┌─────────────────────────────────────────────────────┐
│                   API Gateway                        │
│             (Ocelot / YARP / Kong)                  │
│  - Routing                                          │
│  - Rate Limiting                                    │
│  - Authentication                                   │
│  - Load Balancing                                   │
└──────────────┬──────────────────────────────────────┘
               │
       ┌───────┴───────┐
       │               │
┌──────▼──────┐  ┌────▼─────────┐  ┌──────────────┐
│  Blockchain  │  │   Analytics   │  │   Notification│
│   Service    │  │    Service    │  │    Service    │
├──────────────┤  ├──────────────┤  ├──────────────┤
│ - Fetch Data │  │ - Aggregation│  │ - Webhooks   │
│ - Store Data │  │ - Statistics │  │ - Email/SMS  │
│ - Query Data │  │ - Reports    │  │ - Alerts     │
└──────┬───────┘  └───────┬──────┘  └──────┬───────┘
       │                  │                 │
       └──────────────────┼─────────────────┘
                          │
                ┌─────────▼─────────┐
                │   Message Bus     │
                │  (RabbitMQ/Kafka) │
                └───────────────────┘
                          │
                ┌─────────▼─────────┐
                │  Shared Database  │
                │   (PostgreSQL)    │
                └───────────────────┘
```

**Key Components:**
