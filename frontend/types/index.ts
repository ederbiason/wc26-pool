export type MatchStatus = "NotStarted" | "InProgress" | "Finished";

export type MatchDuration = "REGULAR" | "EXTRA_TIME" | "PENALTY_SHOOTOUT";

export type PenaltySide = "HOME" | "AWAY";

export type PickemRound = "QUARTER_FINAL" | "SEMI_FINAL" | "FINAL";

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
  duration: MatchDuration;
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
  penaltyWinnerTeam: PenaltySide | null;
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

export interface BracketSlot {
  slotIndex: number;
  teamName: string;
  teamFlag: string;
  isEliminated: boolean;
  eliminatedBy: string | null;
}

export interface PickemBracket {
  quarterFinals: BracketSlot[];
  deadline: string;
}

export interface PickemPickSubmission {
  round: PickemRound;
  slotIndex: number;
  chosenTeam: string;
  chosenTeamFlag: string;
}

export interface PickemEntrySubmission {
  participantId: number;
  picks: PickemPickSubmission[];
}

export interface PickemStatus {
  completed: ParticipantSummary[];
  pending: ParticipantSummary[];
  isRevealed: boolean;
}

export interface PickemPick {
  round: PickemRound;
  slotIndex: number;
  chosenTeam: string;
  chosenTeamFlag: string;
  isCorrect: boolean | null;
  pointsEarned: number | null;
}

export interface PickemEntry {
  id: number;
  participantId: number;
  participantName: string;
  createdAt: string;
  isLocked: boolean;
  picks: PickemPick[];
}

export interface ParticipantStats {
  participantId: number;
  participantName: string;
  totalPoints: number;
  totalPredictions: number;
  exactScores: number;
  correctResults: number;
  wrongPredictions: number;
  hitRate: number;
  pointsByStage: {
    groupStage: number;
    knockoutStage: number;
  };
  pickemPoints: number;
}

export interface HighlightEntry {
  participantName: string;
  count?: number;
  rate?: number;
  points?: number;
}

export interface GlobalHighlights {
  mostExactScores: { participantName: string; count: number };
  bestHitRate: { participantName: string; rate: number };
  bestKnockout: { participantName: string; points: number };
  pickemLeader: { participantName: string; points: number };
}

export interface MatchHighlight {
  matchId: number;
  homeTeam: string;
  awayTeam: string;
  homeScore: number | null;
  awayScore: number | null;
  stage: string;
  exactScoreCount: number;
  correctResultCount: number;
}

export interface StatsResponse {
  participants: ParticipantStats[];
  highlights: GlobalHighlights;
  matchHighlights: MatchHighlight[];
}
