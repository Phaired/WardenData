# WardenData

API .NET 9 pour collecter et analyser les données de jeu du système Warden (amélioration d'objets avec des runes).

## Démarrage Rapide

### Prérequis
- .NET 9 SDK
- Docker & Docker Compose
- PostgreSQL (fourni via Docker)

### Installation et Lancement

```bash
# Cloner le projet
git clone <repository-url>
cd WardenData

# Lancer avec Docker Compose (recommandé)
docker compose up

# OU lancer en développement local
dotnet run --project WardenData
```

L'API sera accessible sur `http://localhost:5001`

## API de Statistiques

Le contrôleur `StatisticsController` fournit plusieurs endpoints pour analyser les données d'amélioration d'objets :

### 📊 Taux de Réussite des Runes
```http
GET /api/statistics/rune-success-rates
```

**Paramètres optionnels :**
- `runeId` : ID de rune spécifique
- `effectName` : Filtrer par nom d'effet (ex: "Vitalité")
- `isTenta` : `true` pour tenta, `false` pour normal

**Exemple :**
```bash
curl "http://localhost:5001/api/statistics/rune-success-rates?effectName=Vitalité&isTenta=false"
```

**Réponse :**
```json
[
  {
    "runeId": 15,
    "runeName": "Rune de Vitalité",
    "effectName": "Vitalité",
    "totalAttempts": 120,
    "successes": 45,
    "successRate": 37.5,
    "isTenta": false
  }
]
```

### 📈 Progression des Effets
```http
GET /api/statistics/effect-progression
```

**Paramètres optionnels :**
- `sessionId` : Session spécifique
- `effectName` : Filtrer par effet
- `minValue` / `maxValue` : Plage de valeurs

**Exemple :**
```bash
curl "http://localhost:5001/api/statistics/effect-progression?effectName=Vitalité&minValue=100"
```

### 💰 Efficacité Coût/Réussite
```http
GET /api/statistics/cost-efficiency
```

**Paramètres optionnels :**
- `startDate` / `endDate` : Période (format ISO)
- `minCost` / `maxCost` : Plage de coûts

**Exemple :**
```bash
curl "http://localhost:5001/api/statistics/cost-efficiency?startDate=2024-01-01&endDate=2024-12-31"
```

### ⚖️ Comparaison Tenta vs Normal
```http
GET /api/statistics/tenta-comparison
```

**Paramètres optionnels :**
- `effectName` : Filtrer par effet

**Exemple :**
```bash
curl "http://localhost:5001/api/statistics/tenta-comparison?effectName=Dommages"
```

### 📋 Progression de Session
```http
GET /api/statistics/session-progress/{sessionId}
```

Suit étape par étape l'évolution d'une session d'amélioration.

**Exemple :**
```bash
curl "http://localhost:5001/api/statistics/session-progress/123e4567-e89b-12d3-a456-426614174000"
```

### 📊 Statistiques d'Usage des Runes
```http
GET /api/statistics/rune-usage-stats
```

Montre l'utilisation globale et les taux de réussite par rune.

## Structure des Données

### Modèles Principaux
- **Order** : Objet à améliorer
- **OrderEffect** : Statistiques cibles pour l'objet
- **Session** : Session d'amélioration avec état initial
- **RuneHistory** : Historique détaillé de chaque application de rune

### Types d'Analyse Disponibles

1. **Analyse de Réussite** : Taux de réussite par rune, effet, type (tenta/normal)
2. **Analyse de Progression** : Évolution des valeurs d'effets au fil du temps
3. **Analyse de Coût** : Rapport coût/efficacité des sessions
4. **Analyse Comparative** : Comparaison entre runes tenta et normales
5. **Suivi de Session** : Progression détaillée étape par étape

### Exemples de Requêtes d'Analyse

```bash
# Top des runes les plus efficaces
curl "http://localhost:5001/api/statistics/rune-success-rates" | jq '.[] | select(.successRate > 50)'

# Sessions les plus rentables
curl "http://localhost:5001/api/statistics/cost-efficiency" | jq '.[] | select(.costPerSuccess < 1000)'

# Comparaison tenta vs normal pour Vitalité
curl "http://localhost:5001/api/statistics/tenta-comparison?effectName=Vitalité"
```

## Configuration

### Base de Données
- **Production** : PostgreSQL sur host `database` (Docker)
- **Développement** : PostgreSQL sur `localhost:5432`
- **Utilisateur** : `adm` / **Mot de passe** : `adm`
- **Base** : `warden`

### Commandes Utiles

```bash
# Construction
dotnet build

# Migration de base de données
dotnet ef migrations add <NomMigration> --project WardenData
dotnet ef database update --project WardenData

# Docker
docker compose build        # Construire l'image
docker compose up -d        # Lancer en arrière-plan
docker compose logs -f      # Voir les logs
```

## Architecture

- **Stockage** : PostgreSQL avec JSONB pour données complexes
- **ORM** : Entity Framework Core 9
- **Performance** : EFCore.BulkExtensions pour insertions en masse
- **API** : ASP.NET Core Web API
- **Conteneurisation** : Docker avec images multi-stage