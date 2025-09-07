# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Building and Running
- `dotnet build` - Build the solution
- `dotnet run --project WardenData` - Run the API locally (listens on configured ports)
- `docker compose up` - Run the full application stack with PostgreSQL database

### Database Operations
- `dotnet ef migrations add <MigrationName> --project WardenData` - Create new migration
- `dotnet ef database update --project WardenData` - Apply migrations to database
- Database migrations are automatically applied on application startup

### Docker Operations
- `docker compose build` - Build the Docker image
- `docker compose up -d database` - Start only the PostgreSQL database
- `docker compose down` - Stop all containers

## Architecture Overview

This is a .NET 9 Web API designed to collect and store game data (Warden system) using Entity Framework Core with PostgreSQL.

### Core Components

**Data Models** (`WardenData/Models/`):
- `Order` - Primary entity with UUID server-side ID and original client ID
- `OrderEffect` - Child effects linked to orders
- `Session` - Game sessions with JSONB data storage
- `RuneHistory` - Detailed action history with JSONB effects

**Database Design**:
- Uses UUID primary keys generated server-side
- Maintains original client IDs in `OriginalId` fields for reference
- JSONB columns for complex data structures (`InitialEffects`, `RunesPrices`, `EffectsAfter`)
- Automatic database migration on startup

**API Structure** (`WardenData/Controllers/DataController.cs`):
- Bulk insert/update operations using `EFCore.BulkExtensions`
- DTO pattern for data transfer with original IDs
- Foreign key resolution from original IDs to server UUIDs
- Error handling for missing referenced entities

### Database Connection

- Production: Connects to `database` host (Docker Compose)
- Development: Fallback to `localhost` with default credentials
- Database: PostgreSQL with `warden` database, `adm` user/password

### Key Dependencies

- Entity Framework Core 9.0.1 with PostgreSQL provider
- EFCore.BulkExtensions for performance
- System.Text.Json for JSONB handling
- Docker support with multi-stage builds

## Data Analysis and Schema

### Data Flow for Game Item Enhancement Analysis

The system tracks the progression of game item enhancements through rune applications:

**Order Schema** (Item to enhance):
```json
{
  "id": 123,
  "name": "Item Name"
}
```

**OrderEffect Schema** (Target stats for enhancement):
```json
{
  "id": 456,
  "OrderId": 123,
  "EffectName": "Vitalité",
  "MinValue": 100,
  "MaxValue": 200,
  "DesiredValue": 150
}
```

**Session Schema** (Enhancement session):
```json
{
  "id": 789,
  "OrderId": 123,
  "timestamp": 1704067200000,
  "InitialEffects": "{\"effects\":[{\"effect_name\":\"Vitalité\",\"current_value\":120,...}]}",
  "RunesPrices": "{\"rune_prices\":{\"1\":1500,\"2\":2000,...}}"
}
```

**RuneHistory Schema** (Individual rune application results):
```json
{
  "id": 101112,
  "session_id": 789,
  "rune_id": 15,
  "is_tenta": true,
  "effects_after": {
    "effects": [
      {
        "effect_name": "Vitalité",
        "current_value": 135,
        "max_value": 200,
        "min_value": 100,
        "desired_value": 150,
        "weight": 1.0,
        "index_on_item": 0
      }
    ]
  },
  "has_succeed": false
}
```

### Analysis Capabilities

For data analysis queries, the system can provide:

- **Success Rate Analysis**: Track `has_succeed` rates by rune type, effect type, or value ranges
- **Cost Efficiency**: Correlate rune prices from `RunesPrices` with success outcomes
- **Progress Tracking**: Monitor value progression from `InitialEffects` through `RuneHistory.effects_after`
- **Tenta vs Regular Runes**: Compare success rates using `is_tenta` flag
- **Session Performance**: Analyze complete enhancement sessions from start to finish

### JSONB Query Patterns

When querying JSONB data:
- Use `->` for JSON object access: `initial_effects->'effects'`
- Use `->>` for text extraction: `effects_after->>'current_value'`
- Use `@>` for containment queries: `effects_after @> '{"effects":[{"effect_name":"Vitalité"}]}'`

## Development Notes

- The system handles ID mapping from client-generated IDs to server UUIDs
- All entities follow the pattern: server UUID + original client ID
- Foreign key relationships require resolving original IDs to server UUIDs
- JSONB is used extensively for storing complex game state data
- Bulk operations are preferred for data ingestion performance