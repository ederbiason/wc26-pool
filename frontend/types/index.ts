export type MatchStatus = "NotStarted" | "InProgress" | "Finished";

export interface Match {
  id: number;
  externalId: string;
  homeTeam: string;
  awayTeam: string;
  homeTeamFlag: string;
  awayTeamFlag: string;
  matchDate: string;
  status: MatchStatus;
  homeScore: number | null;
  awayScore: number | null;
  pointsCalculated: boolean;
}

export interface Prediction {
  id: number;
  participantId: number;
  participantName: string;
  matchId: number;
  predictedHomeScore: number;
  predictedAwayScore: number;
  createdAt: string;
  pointsEarned: number | null;
}

export interface PredictionsDay {
  revealed: boolean;
  predictions: Prediction[];
}

export interface DayPredictionOrder {
  participantId: number;
  participantName: string;
  order: number;
  hasSubmittedAll: boolean;
}

export interface Participant {
  id: number;
  name: string;
  isAdmin: boolean;
  totalPoints: number;
}

export interface RankingEntry {
  position: number;
  participantId: number;
  name: string;
  totalPoints: number;
}

export interface StoredIdentity {
  participantId: number;
  participantName: string;
  isAdmin: boolean;
}
