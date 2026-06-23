"use client";

import { useCallback } from "react";
import type { Match, Prediction, DayPredictionOrder } from "@/types";
import { useIdentity } from "@/components/IdentityProvider";
import { PredictionForm } from "@/components/PredictionForm";
import { formatTimeBrasilia, myPredictionForMatch } from "@/lib/api";

interface Props {
  match: Match;
  predictions: Prediction[];
  revealed: boolean;
  order: DayPredictionOrder[];
  onPredicted: () => void;
}

function StatusBadge({ status }: { status: Match["status"] }) {
  if (status === "NotStarted")
    return (
      <span className="inline-flex items-center gap-1 text-[10px] font-bold uppercase tracking-widest text-brand-gold bg-brand-gold/10 rounded-full px-2 py-0.5">
        <span className="w-1.5 h-1.5 rounded-full bg-brand-gold animate-pulse" />
        Em breve
      </span>
    );
  if (status === "InProgress")
    return (
      <span className="inline-flex items-center gap-1 text-[10px] font-bold uppercase tracking-widest text-green-400 bg-green-400/10 rounded-full px-2 py-0.5">
        <span className="w-1.5 h-1.5 rounded-full bg-green-400 animate-ping" />
        Ao vivo
      </span>
    );
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
  const isLeft = align === "left";
  return (
    <div
      className={`flex flex-col items-center gap-1.5 w-[90px] ${
        isLeft ? "text-left" : "text-right"
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
      <div className="flex items-center gap-2 min-w-[80px] justify-center">
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

function PredictionRow({ prediction }: { prediction: Prediction }) {
  return (
    <div className="flex items-center justify-between text-xs">
      <span className="text-[#86B59A] truncate max-w-[100px]">
        {prediction.participantName}
      </span>
      <span className="font-bold text-[#E8F5E9] tabular-nums">
        {prediction.predictedHomeScore} × {prediction.predictedAwayScore}
      </span>
      {prediction.pointsEarned !== null && (
        <span className="text-brand-gold font-bold text-xs ml-2">
          +{prediction.pointsEarned}
        </span>
      )}
    </div>
  );
}

function HiddenPredictionRow({ participantName }: { participantName: string }) {
  return (
    <div className="flex items-center justify-between text-xs">
      <span className="text-[#86B59A] truncate max-w-[100px]">
        {participantName}
      </span>
      <span className="text-[#1E4A32] font-bold tracking-widest">? × ?</span>
    </div>
  );
}

export function MatchCard({
  match,
  predictions,
  revealed,
  order,
  onPredicted,
}: Props) {
  const { identity } = useIdentity();

  const myPrediction = identity
    ? myPredictionForMatch(predictions, match.id, identity.participantId)
    : undefined;

  const currentTurn = order.find((o) => !o.hasSubmittedAll);
  const isMyTurn =
    !currentTurn || currentTurn.participantId === identity?.participantId;

  const canPredict =
    match.status === "NotStarted" && !myPrediction && isMyTurn;

  const showWaiting =
    match.status === "NotStarted" && !myPrediction && !isMyTurn;

  const handlePredicted = useCallback(() => {
    onPredicted();
  }, [onPredicted]);

  return (
    <div className="bg-brand-surface rounded-2xl border border-[#1E4A32] overflow-hidden">
      <div className="flex items-center justify-between px-4 pt-3 pb-2">
        <StatusBadge status={match.status} />
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

      {(predictions.length > 0 || revealed || myPrediction) && (
        <div className="border-t border-[#1E4A32] px-4 py-3 flex flex-col gap-1.5">
          <span className="text-[#86B59A] text-[10px] font-bold uppercase tracking-widest mb-1">
            Palpites
          </span>
          {revealed ? (
            predictions.map((p) => (
              <PredictionRow key={p.id} prediction={p} />
            ))
          ) : myPrediction ? (
            <PredictionRow prediction={myPrediction} />
          ) : (
            order.map((o) => (
              <HiddenPredictionRow
                key={o.participantId}
                participantName={o.participantName}
              />
            ))
          )}
        </div>
      )}

      {match.status !== "NotStarted" && (
        <div className="px-4 pb-3">
          <span className="text-[#86B59A] text-xs">Palpites encerrados</span>
        </div>
      )}

      {showWaiting && (
        <div className="px-4 pb-3 border-t border-[#1E4A32] pt-3">
          <p className="text-[#86B59A] text-xs">
            Aguardando vez de{" "}
            <span className="text-brand-gold font-semibold">
              {currentTurn?.participantName}
            </span>
            …
          </p>
        </div>
      )}

      {canPredict && (
        <div className="px-4 pb-4">
          <PredictionForm matchId={match.id} onSuccess={handlePredicted} />
        </div>
      )}
    </div>
  );
}
