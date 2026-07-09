# wc26-pool 🏆

A personal World Cup 2026 prediction pool built for a group of six friends who were tired of managing everything manually through WhatsApp.

## The Problem

Every day during the tournament, someone had to manually send the day's matches to the group chat, each person replied with their predictions, and after the games someone else had to calculate points and update the rankings by hand. It worked, but it was tedious — and easy to lose track of who predicted what.

We wanted something more organized, but signing up for an existing app felt overkill for six people who just wanted a clean, personalized experience built around their own rules.

## The Solution

A lightweight full-stack web app where each participant can submit predictions, track live scores, follow the standings, and see everyone's picks — all from their phone, without creating accounts or remembering passwords.

## Features

- 📅 **Daily match schedule** with live scores updated in real time
- 🔮 **Prediction system** with immutable submissions — once sent, it's locked
- 👁️ **Auto-reveal logic** — predictions stay hidden until all six participants submit, or the match kicks off
- 📊 **Live ranking** updated automatically after each match ends
- 📆 **Upcoming matches calendar** showing the next 7 days grouped by date
- ⚽ **Knockout stage support** with extra time and penalty shootout predictions
- 🔁 **Match revalidation** — score corrections after a result is posted are automatically detected and points are recalculated
- 📱 **Mobile-first design** since everyone accesses it from their phones

## Scoring Rules

### Group Stage
| Outcome | Points |
|---|---|
| Exact scoreline | 2 pts |
| Correct result (win/draw/loss) | 1 pt |
| Wrong result | 0 pts |

### Knockout Stage (Round of 32 onwards)
| Outcome | Points |
|---|---|
| Correct scoreline (full time including extra time) + correct penalty winner | 3 pts |
| Correct scoreline (full time including extra time) | 2 pts |
| Predicted draw + correct penalty winner (game went to penalties) | 2 pts |
| Correct team advancing (regardless of how) | 1 pt |
| Predicted draw in a game that went to penalties (even if wrong winner) | 1 pt |
| Wrong team advancing | 0 pts |

Points are never cumulative — only the highest applicable tier is awarded per match.

When predicting a draw in the knockout stage, selecting the penalty shootout winner becomes mandatory, since a draw must be resolved. The backend enforces this validation and rejects draw predictions without a penalty winner.

## Tech Stack

### Backend — ASP.NET Core 9
The main reason for choosing .NET was learning. This project was an opportunity to take my first real steps with C# and ASP.NET Core in a low-stakes environment with a concrete domain I already understood well.

- **ASP.NET Core 9** with Minimal APIs
- **Entity Framework Core 9** with Npgsql for PostgreSQL
- **BackgroundService** running a smart polling worker that fetches live match data from [football-data.org](https://football-data.org)
- Automatic database migrations on startup

### Frontend — Next.js 15
Already familiar with the stack, which meant I could build the interface quickly and focus on the backend learning.

- **Next.js 15** with App Router
- **TypeScript**
- **Tailwind CSS** + **shadcn/ui**
- Client-side polling with automatic refetch during live matches

### Infrastructure
- **Heroku** (Eco Dyno + Heroku Postgres Mini) for the backend
- **Vercel** for the frontend
- Both are provisioned under the [GitHub Student Developer Pack](https://education.github.com/pack)

## Architecture Decisions

### No login system
Six people, one group, all known to each other. Adding authentication would have meant either managing passwords (annoying) or OAuth flows (overkill). Instead, participants select their name on first visit and the choice is persisted in `localStorage`. A "switch user" option exists for corrections.

### Backend as the API gateway for football data
The frontend never calls the football data API directly. The backend runs a `BackgroundService` that polls the external API on a smart schedule and caches everything in PostgreSQL. This means:

- All six users share a single API quota
- The frontend polls the backend (no rate limit), not the external API
- The polling interval adapts dynamically based on match state

**Polling intervals:**
- No matches today or all finished → sleep until midnight
- Next match in more than 2 hours → every 20 minutes
- Next match in 30 minutes to 2 hours → every 10 minutes
- Next match in less than 30 minutes → every 2 minutes
- Match overdue (should have started but status not updated) → every 1 minute
- Match in progress → every 1 minute

The service also detects overdue matches — `NotStarted` matches whose kickoff time has already passed — and immediately fetches today's full data from the API to resolve the status.

This keeps daily API usage well within the free tier's 10 requests/minute limit.

### Match finish revalidation and confirmation delay
External football APIs can briefly return `FINISHED` for a match while the score is still being finalized. To avoid awarding points based on a transient or incorrect result, the system uses a multi-stage confirmation flow:

1. **First `Finished` detection**: The timestamp is recorded in `FinishedDetectedAt` but no points are calculated yet.
2. **2-minute stabilization window**: If the match is still `Finished` on the next polling cycle and at least 2 minutes have elapsed, points are calculated.
3. **Score unstable on first detection**: If the score also changed in the same cycle the match transitioned to `Finished`, the detection is reset and the cycle starts over.
4. **Reverted to `InProgress`**: If the API walks back the `Finished` status (extra time added, etc.), `FinishedDetectedAt` is cleared, `PointsCalculated` is set to `false`, and the match re-enters the confirmation flow from scratch.
5. **Score correction after points are calculated**: If a `Finished` match's score changes after points were already awarded, the system detects the drift, zeroes out all affected predictions, and recalculates points with the corrected data.

### Immutable predictions
Once a prediction is submitted it cannot be changed, mirroring how the group operated on WhatsApp. The backend enforces this with a unique constraint on `(ParticipantId, MatchId)` and rejects duplicate submissions with a 409 response.

### Auto-reveal logic
Predictions are hidden from other participants until either all six have submitted for that specific match, or the match kicks off (or is already finished) — whichever comes first. This prevents anyone from copying someone else's pick. The visibility state is computed dynamically on every request, with no persistent toggle needed.

### Knockout stage adaptation
Midway through the tournament the group added new scoring rules for the knockout rounds. The system was extended to track `Duration` (REGULAR / EXTRA_TIME / PENALTY_SHOOTOUT), `RegularTimeHomeScore`/`RegularTimeAwayScore`, and `PenaltyHomeScore`/`PenaltyAwayScore` separately, matching the data structure returned by the football-data.org API. The scoring service branches on the match `Stage` field to apply the correct rule set.

`HomeScore`/`AwayScore` always reflect goals through regular + extra time only — penalty goals are stored separately and never added to the main scoreline.

### Timezone handling
All date calculations use Brasília time (America/Sao\_Paulo) for determining "today", midnight resets, and grouping matches by display day. This is centralized in the `BrasiliaTime` helper class to avoid timezone bugs scattered across the codebase.

## Project Structure

```
wc26-pool/
├── backend/
│   └── WC26Pool.API/
│       ├── BackgroundServices/   # Football polling worker
│       ├── Data/                 # EF Core DbContext and migrations
│       ├── DTOs/                 # Request and response models
│       ├── Endpoints/            # Minimal API route handlers
│       ├── Helpers/              # BrasiliaTime timezone utilities
│       ├── Models/               # Domain entities
│       ├── Services/             # Business logic (scoring, visibility)
│       └── Program.cs
└── frontend/
    ├── app/                      # Next.js App Router pages
    ├── components/               # React components
    ├── lib/                      # API client and localStorage helpers
    └── types/                    # TypeScript interfaces
```

## API Endpoints

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/matches/today` | Today's matches with prediction visibility |
| `GET` | `/api/matches/day/{date}` | Matches for a specific date (yyyy-MM-dd) |
| `GET` | `/api/matches/upcoming` | Next 7 days of matches grouped by date |
| `GET` | `/api/matches/{id}` | Single match with visibility |
| `GET` | `/api/predictions/day/{date}` | Predictions for a specific date |
| `POST` | `/api/predictions` | Submit a prediction |
| `GET` | `/api/participants` | List all participants |
| `GET` | `/api/ranking` | Current standings |
| `POST` | `/api/admin/sync-upcoming` | Manually trigger an upcoming matches sync |

The `X-Participant-Id` request header is used to scope prediction visibility — each user only sees their own picks until the reveal condition is met.

## Running Locally

### Prerequisites
- .NET 9 SDK
- Node.js 18+
- PostgreSQL
- A free API key from [football-data.org](https://www.football-data.org/client/register)

### Backend

```bash
cd backend/WC26Pool.API

# Set up user secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=wc26pool_dev;Username=postgres;Password=postgres"
dotnet user-secrets set "FootballApi:ApiKey" "YOUR_API_KEY"

# Apply migrations and start
dotnet ef database update
dotnet watch run
```

### Frontend

```bash
cd frontend
cp .env.example .env.local
# Set NEXT_PUBLIC_API_URL=http://localhost:5000

npm install
npm run dev
```

## Participants

Pedro · Gabriel · Vinícius · João · Guilherme · Eder