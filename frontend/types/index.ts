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
  duration: "REGULAR" | "EXTRA_TIME" | "PENALTY_SHOOTOUT";
  stage: string;
  penaltyHomeScore: number | null;
  penaltyAwayScore: number | null;
  regularTimeHomeScore: number | null;
  regularTimeAwayScore: number | null;
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
  penaltyWinnerTeam: "HOME" | "AWAY" | null;
}

export interface ParticipantSummary {
  participantId: number;
  participantName: string;
}

export interface PredictionVisibility {
  isRevealed: boolean;
  predictions: Prediction[];
  completedParticipants: ParticipantSummary[];
  pendingParticipants: ParticipantSummary[];
}

export interface MatchWithVisibility extends Match {
  predictionVisibility: PredictionVisibility;
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

export interface UpcomingDay {
  date: string;
  matches: Match[];
}

export interface StoredIdentity {
  participantId: number;
  participantName: string;
  isAdmin: boolean;
}
