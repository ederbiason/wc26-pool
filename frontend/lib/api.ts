import { getIdentity } from "@/lib/storage";
import type {
  Match,
  MatchWithVisibility,
  UpcomingDay,
  Participant,
  RankingEntry,
} from "@/types";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

function buildHeaders(): HeadersInit {
  const identity = getIdentity();
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
  };
  if (identity) {
    headers["X-Participant-Id"] = String(identity.participantId);
  }
  return headers;
}

async function get<T>(path: string): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    headers: buildHeaders(),
    cache: "no-store",
  });
  if (!res.ok) throw new Error(`GET ${path} failed: ${res.status}`);
  return res.json();
}

async function post<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    method: "POST",
    headers: buildHeaders(),
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || `POST ${path} failed: ${res.status}`);
  }
  return res.json();
}

export const api = {
  getParticipants: () => get<Participant[]>("/api/participants"),

  getMatchesToday: () => get<MatchWithVisibility[]>("/api/matches/today"),

  getMatchesForDay: (date: string) =>
    get<MatchWithVisibility[]>(`/api/matches/day/${date}`),

  getMatchesUpcoming: () => get<UpcomingDay[]>("/api/matches/upcoming"),

  getPredictionsForDay: (date: string) =>
    get<MatchWithVisibility[]>(`/api/predictions/day/${date}`),

  submitPrediction: (payload: {
    participantId: number;
    matchId: number;
    predictedHomeScore: number;
    predictedAwayScore: number;
    penaltyWinnerTeam: "HOME" | "AWAY" | null;
  }) => post<{ id: number }>("/api/predictions", payload),

  getRanking: () => get<RankingEntry[]>("/api/ranking"),
};

export function formatTimeBrasilia(utcDateString: string): string {
  return new Date(utcDateString).toLocaleTimeString("pt-BR", {
    timeZone: "America/Sao_Paulo",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function formatDateBrasilia(utcDateString: string): string {
  return new Date(utcDateString).toLocaleDateString("pt-BR", {
    timeZone: "America/Sao_Paulo",
    weekday: "long",
    day: "numeric",
    month: "long",
  });
}

export function todayDateParam(): string {
  return new Date().toLocaleDateString("sv-SE", {
    timeZone: "America/Sao_Paulo",
  });
}
