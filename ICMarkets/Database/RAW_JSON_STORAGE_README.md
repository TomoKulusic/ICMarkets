# Raw JSON Storage Implementation

## Overview
This implementation stores blockchain data from the BlockCypher API as raw JSON strings, preserving the exact response format from the API.

## Database Schema

The `BlockchainData` table now has the following structure:

| Column | Type | Description |
|--------|------|-------------|
| Id | int | Primary key (auto-increment) |
| Chain | string | Blockchain type (eth, btc, dash, ltc) |
| Network | string | Network type (main, test3) |
| RawJsonData | string | Complete JSON response from BlockCypher API |
| CreatedAt | DateTime | Timestamp when data was fetched from API |

## Benefits of Raw JSON Storage

1. **Data Integrity**: Stores data exactly as provided by the API
2. **Flexibility**: No schema changes needed when API adds new fields
3. **Simplicity**: Simpler database structure
4. **Completeness**: No risk of missing fields during mapping
5. **History**: Complete historical record with timestamps

## API Endpoints

### GET /api/blockchain
Returns all blockchain data ordered by CreatedAt descending.

**Response Example:**
```json
[
  {
    "id": 1,
    "chain": "eth",
    "network": "main",
    "rawJsonData": "{\"name\":\"ETH.main\",\"height\":23949057,...}",
    "createdAt": "2025-12-05T21:07:44.006Z"
  }
]
```

### GET /api/blockchain/{chain}
Returns blockchain data for a specific chain ordered by CreatedAt descending.

### GET /api/blockchain/{chain}/latest
Returns the most recent blockchain data entry for a specific chain.

### POST /api/blockchain/fetch
Fetches fresh data from BlockCypher API and stores it.

**Query Parameters:**
- `chain`: eth, btc, dash, ltc
- `network`: main, test3

### POST /api/blockchain/fetch-all
Fetches data for all supported blockchains in parallel:
- eth/main
- dash/main
- btc/main
- btc/test3
- ltc/main

## Parsing the JSON Data

When retrieving data, parse the `RawJsonData` field to access individual properties:

```csharp
using System.Text.Json;

var blockchainData = await GetLatest("eth");
var apiResponse = JsonSerializer.Deserialize<JsonElement>(blockchainData.RawJsonData);

var height = apiResponse.GetProperty("height").GetInt64();
var hash = apiResponse.GetProperty("hash").GetString();
var time = apiResponse.GetProperty("time").GetDateTime();
```

## Migration Notes

### New Installations
The database will be created automatically with the correct schema using `EnsureCreated()`.

### Existing Databases

**Option 1: Fresh Start (Recommended)**
1. Stop the application
2. Delete the existing SQLite database file
3. Restart the application (new schema will be created)
4. Use `/api/blockchain/fetch-all` to populate with fresh data

**Option 2: Manual Migration**
Run the migration script provided in `Database/Migration_RawJsonStorage.sql`

## Example BlockCypher API Response

```json
{
  "name": "ETH.main",
  "height": 23949057,
  "hash": "72f6f9f44d37b402ac3a70c3686bc59cbd753d18badc06a1d0b9e4c2702ba663",
  "time": "2025-12-05T21:07:44.006400798Z",
  "latest_url": "https://api.blockcypher.com/v1/eth/main/blocks/...",
  "previous_hash": "002d1492bb8c0e63e9ffcb776ed0f8bf62b3c1a773aed64f1c07c78c048020a4",
  "peer_count": 0,
  "unconfirmed_count": 490,
  "high_gas_price": 15006218050,
  "medium_gas_price": 9183757421,
  "low_gas_price": 2779139940,
  "base_fee": 22041954,
  "last_fork_height": 23932978,
  "last_fork_hash": "b718f23e74801fc9ebe33add0ea3cb104ce566f0a599c85a60fb76d2eade074a"
}
```

This complete response is stored in the `RawJsonData` field.

## Testing

1. Fetch data for a single blockchain:
   ```bash
   POST http://localhost:5000/api/blockchain/fetch?chain=eth&network=main
   ```

2. Fetch all blockchains:
   ```bash
   POST http://localhost:5000/api/blockchain/fetch-all
   ```

3. Get all historical data:
   ```bash
   GET http://localhost:5000/api/blockchain
   ```

4. Get latest for specific chain:
   ```bash
   GET http://localhost:5000/api/blockchain/eth/latest
   ```
