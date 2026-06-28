"use client";

import { useCallback, useState } from "react";
import type { Match, PredictionVisibility, Prediction } from "@/types";
import { useIdentity } from "@/components/IdentityProvider";
import { PredictionForm } from "@/components/PredictionForm";
import { formatTimeBrasilia } from "@/lib/api";

interface Props {
  match: Match;
  visibility: PredictionVisibility | null;
  onPredicted: () => void;
}

function StatusBadge({ match }: { match: Match }) {
  if (match.status === "NotStarted")
    return (
      <span className="inline-flex items-center gap-1 text-[10px] font-bold uppercase tracking-widest text-brand-gold bg-brand-gold/10 rounded-full px-2 py-0.5">
        <span className="w-1.5 h-1.5 rounded-full bg-brand-gold animate-pulse" />
        Em breve
      </span>
    );
  if (match.status === "InProgress") {
    const label = match.duration === "EXTRA_TIME" ? "Prorrogação" : match.duration === "PENALTY_SHOOTOUT" ? "Pênaltis" : "Ao vivo";
    return (
      <span className="inline-flex items-center gap-1 text-[10px] font-bold uppercase tracking-widest text-green-400 bg-green-400/10 rounded-full px-2 py-0.5">
        <span className="w-1.5 h-1.5 rounded-full bg-green-400 animate-ping" />
        {label}
      </span>
    );
  }
  return (
    <span className="inline-flex items-center gap-1 text-[10px] font-bold uppercase tracking-widest text-[#86B59A] bg-[#86B59A]/10 rounded-full px-2 py-0.5">
      <span className="w-1.5 h-1.5 rounded-full bg-[#86B59A]" />
      Encerrado
    </span>
  );
}

function TeamBlock({
  name,
  flagUrl,
  align,
}: {
  name: string;
  flagUrl: string;
  align: "left" | "right";
}) {
  return (
    <div
      className={`flex flex-col items-center gap-1.5 w-[90px] ${
        align === "left" ? "text-left" : "text-right"
      }`}
    >
      <div className="w-12 h-12 rounded-xl overflow-hidden bg-brand-surface2 flex items-center justify-center shadow-lg">
        {flagUrl ? (
          <img
            src={flagUrl}
            alt={name}
            className="w-full h-full object-cover"
            onError={(e) => {
              (e.target as HTMLImageElement).style.display = "none";
            }}
          />
        ) : (
          <span className="text-2xl">🏳️</span>
        )}
      </div>
      <span className="text-[#E8F5E9] text-xs font-semibold text-center leading-tight line-clamp-2">
        {name}
      </span>
    </div>
  );
}

function ScoreDisplay({ match }: { match: Match }) {
  const hasScore =
    match.status !== "NotStarted" &&
    match.homeScore !== null &&
    match.awayScore !== null;

  if (hasScore) {
    return (
      <div className="flex flex-col items-center gap-1 min-w-[80px]">
        <div className="flex items-center gap-2 justify-center">
          <span className="font-display text-5xl text-brand-gold leading-none tabular-nums">
            {match.homeScore}
          </span>
          <span className="font-display text-2xl text-[#1E4A32] leading-none">
            ×
          </span>
          <span className="font-display text-5xl text-brand-gold leading-none tabular-nums">
            {match.awayScore}
          </span>
        </div>
        {match.duration === "EXTRA_TIME" && (
          <span className="text-brand-gold text-[10px] font-bold uppercase tracking-widest bg-brand-gold/10 rounded px-1.5 py-0.5">
            Prorrogação
          </span>
        )}
        {match.duration === "PENALTY_SHOOTOUT" && (
          <span className="text-brand-gold text-[10px] font-bold uppercase tracking-widest bg-brand-gold/10 rounded px-1.5 py-0.5">
            Pênaltis: {match.penaltyHomeScore ?? 0} × {match.penaltyAwayScore ?? 0}
          </span>
        )}
      </div>
    );
  }

  return (
    <div className="flex flex-col items-center gap-0.5 min-w-[80px]">
      <span className="font-display text-2xl text-[#E8F5E9] leading-none tracking-wider">
        VS
      </span>
      <span className="text-[#86B59A] text-xs">
        {formatTimeBrasilia(match.matchDate)}
      </span>
    </div>
  );
}

function RevealedPredictionRow({ prediction, match }: { prediction: Prediction; match: Match }) {
  const penaltyTeamName = prediction.penaltyWinnerTeam === "HOME" ? match.homeTeam : prediction.penaltyWinnerTeam === "AWAY" ? match.awayTeam : null;

  return (
    <div className="flex flex-col text-xs py-0.5 min-w-0 gap-0.5">
      <span className="text-[#E8F5E9] font-medium truncate">
        {prediction.participantName}
      </span>
      <div className="flex items-center gap-1.5">
        <span className="font-bold text-brand-gold tabular-nums flex-none">
          {prediction.predictedHomeScore} × {prediction.predictedAwayScore}
        </span>
        {penaltyTeamName && (
          <span className="text-brand-gold text-[10px] font-semibold bg-brand-gold/10 rounded px-1 truncate">
            🥅 {penaltyTeamName}
          </span>
        )}
        {prediction.pointsEarned !== null && (
          <span className="text-green-400 font-bold text-[10px] bg-green-400/10 rounded px-1 flex-none">
            +{prediction.pointsEarned}
          </span>
        )}
      </div>
    </div>
  );
}

function ParticipantStatusRow({
  name,
  done,
}: {
  name: string;
  done: boolean;
}) {
  return (
    <div className="flex items-center gap-2 text-xs py-0.5">
      <span className="text-base leading-none flex-none">
        {done ? "✅" : "⏳"}
      </span>
      <span
        className={`truncate ${
          done ? "text-[#E8F5E9]" : "text-[#86B59A]"
        }`}
      >
        {name}
      </span>
    </div>
  );
}

export function MatchCard({ match, visibility, onPredicted }: Props) {
  const { identity } = useIdentity();
  const [localPrediction, setLocalPrediction] = useState<{
    home: number;
    away: number;
    penaltyWinnerTeam: "HOME" | "AWAY" | null;
  } | null>(null);

  const myServerPrediction = identity
    ? visibility?.predictions.find(
        (p) => p.participantId === identity.participantId
      )
    : undefined;

  const myPrediction = myServerPrediction ?? (localPrediction
    ? {
        id: -1,
        participantId: identity?.participantId ?? 0,
        participantName: identity?.participantName ?? "",
        matchId: match.id,
        predictedHomeScore: localPrediction.home,
        predictedAwayScore: localPrediction.away,
        createdAt: new Date().toISOString(),
        pointsEarned: null,
        penaltyWinnerTeam: localPrediction.penaltyWinnerTeam,
      }
    : undefined);

  const isRevealed = visibility?.isRevealed ?? false;

  const completedIds = new Set(
    visibility?.completedParticipants.map((p) => p.participantId) ?? []
  );

  const allParticipants = [
    ...(visibility?.completedParticipants ?? []),
    ...(visibility?.pendingParticipants ?? []),
  ];

  const canPredict = match.status === "NotStarted" && !myPrediction;

  const handlePredicted = useCallback(
    (home: number, away: number, penaltyWinnerTeam: "HOME" | "AWAY" | null) => {
      setLocalPrediction({ home, away, penaltyWinnerTeam });
      onPredicted();
    },
    [onPredicted]
  );

  const showPredictionsSection = true;

  return (
    <div className="bg-brand-surface rounded-2xl border border-[#1E4A32] overflow-hidden">
      <div className="flex items-center justify-between px-4 pt-3 pb-2">
        <StatusBadge match={match} />
        {match.status === "NotStarted" && (
          <span className="text-[#86B59A] text-xs">
            {formatTimeBrasilia(match.matchDate)}
          </span>
        )}
      </div>

      <div className="flex items-center justify-between px-4 pb-4">
        <TeamBlock
          name={match.homeTeam}
          flagUrl={match.homeTeamFlag}
          align="left"
        />
        <ScoreDisplay match={match} />
        <TeamBlock
          name={match.awayTeam}
          flagUrl={match.awayTeamFlag}
          align="right"
        />
      </div>

      {showPredictionsSection && (
        <div className="border-t border-[#1E4A32] px-4 py-3 flex flex-col gap-0.5">
          <span className="text-[#86B59A] text-[10px] font-bold uppercase tracking-widest mb-1.5">
            Palpites
          </span>

          {isRevealed ? (
            <>
              {visibility?.predictions.length === 0 && !myPrediction ? (
                <p className="text-[#86B59A] text-xs italic">
                  Nenhum palpite foi feito ainda.
                </p>
              ) : (
                <div className="grid grid-cols-2 gap-x-3 gap-y-0.5">
                  {visibility?.predictions.map((p) => (
                    <RevealedPredictionRow key={p.id} prediction={p} match={match} />
                  ))}
                  {myPrediction && !myServerPrediction && (
                    <RevealedPredictionRow prediction={myPrediction} match={match} />
                  )}
                </div>
              )}
            </>
          ) : (
            <>
              {allParticipants.length === 0 && !myPrediction ? (
                <p className="text-[#86B59A] text-xs italic">
                  Nenhum palpite foi feito ainda.
                </p>
              ) : (
                <div className="grid grid-cols-2 gap-x-3 gap-y-0.5">
                  {allParticipants.map((p) => {
                    const isMe = p.participantId === identity?.participantId;
                    const done = completedIds.has(p.participantId);

                    if (isMe && myPrediction) {
                      return (
                        <div
                          key={p.participantId}
                          className="flex items-center gap-1.5 text-xs py-0.5 min-w-0"
                        >
                          <span className="text-sm leading-none flex-none">✅</span>
                          <span className="text-brand-gold font-semibold truncate">
                            {p.participantName}
                          </span>
                          <div className="flex items-center gap-1.5 ml-auto flex-none">
                            <span className="font-bold text-brand-gold tabular-nums">
                              {myPrediction.predictedHomeScore}×{myPrediction.predictedAwayScore}
                            </span>
                            {myPrediction.penaltyWinnerTeam && (
                              <span className="text-brand-gold text-[10px] font-semibold bg-brand-gold/10 rounded px-1">
                                🥅 {myPrediction.penaltyWinnerTeam === "HOME" ? match.homeTeam : match.awayTeam}
                              </span>
                            )}
                          </div>
                        </div>
                      );
                    }

                    return (
                      <ParticipantStatusRow
                        key={p.participantId}
                        name={p.participantName}
                        done={done}
                      />
                    );
                  })}

                  {myPrediction && allParticipants.length === 0 && (
                    <div className="flex items-center gap-1.5 text-xs py-0.5 min-w-0">
                      <span className="text-sm leading-none flex-none">✅</span>
                      <span className="text-brand-gold font-semibold truncate">
                        {myPrediction.participantName}
                      </span>
                      <div className="flex items-center gap-1.5 ml-auto flex-none">
                        <span className="font-bold text-brand-gold tabular-nums">
                          {myPrediction.predictedHomeScore}×{myPrediction.predictedAwayScore}
                        </span>
                        {myPrediction.penaltyWinnerTeam && (
                          <span className="text-brand-gold text-[10px] font-semibold bg-brand-gold/10 rounded px-1">
                            🥅 {myPrediction.penaltyWinnerTeam === "HOME" ? match.homeTeam : match.awayTeam}
                          </span>
                        )}
                      </div>
                    </div>
                  )}
                </div>
              )}
            </>
          )}
        </div>
      )}

      {match.status !== "NotStarted" && (
        <div className="px-4 pb-4">
          <span className="text-[#86B59A] text-xs">Palpites encerrados</span>
        </div>
      )}

      {canPredict && (
        <div className="px-4 pb-4">
          <PredictionForm match={match} onSuccess={handlePredicted} />
        </div>
      )}
    </div>
  );
}
