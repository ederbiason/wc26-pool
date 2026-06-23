import { getIdentity } from "@/lib/storage";
import type {
  Match,
  Participant,
  Prediction,
  PredictionsDay,
  DayPredictionOrder,
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

  getMatchesToday: () => get<Match[]>("/api/matches/today"),

  getMatchesUpcoming: () => get<Match[]>("/api/matches/upcoming"),

  getPredictionsForDay: (date: string) =>
    get<PredictionsDay>(`/api/predictions/day/${date}`),

  getPredictionOrder: (date: string) =>
    get<DayPredictionOrder[]>(`/api/predictions/order/${date}`),

  submitPrediction: (payload: {
    participantId: number;
    matchId: number;
    predictedHomeScore: number;
    predictedAwayScore: number;
  }) => post<{ id: number }>("/api/predictions", payload),

  getRanking: () => get<RankingEntry[]>("/api/ranking"),

  revealPredictions: (date: string) =>
    post<{ message: string }>(`/api/admin/reveal/${date}`),
};

export function matchDateToBrasilia(utcDateString: string): Date {
  return new Date(utcDateString);
}

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
  return new Date()
    .toLocaleDateString("sv-SE", { timeZone: "America/Sao_Paulo" });
}

export function myPredictionForMatch(
  predictions: Prediction[],
  matchId: number,
  participantId: number
): Prediction | undefined {
  return predictions.find(
    (p) => p.matchId === matchId && p.participantId === participantId
  );
}
