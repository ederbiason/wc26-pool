"use client";

import {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  type ReactNode,
} from "react";
import type { StoredIdentity } from "@/types";
import { getIdentity, saveIdentity, clearIdentity } from "@/lib/storage";
import { ParticipantSelector } from "@/components/ParticipantSelector";

interface IdentityContextValue {
  identity: StoredIdentity | null;
  setIdentity: (identity: StoredIdentity) => void;
  logout: () => void;
}

const IdentityContext = createContext<IdentityContextValue | null>(null);

export function useIdentity(): IdentityContextValue {
  const ctx = useContext(IdentityContext);
  if (!ctx) throw new Error("useIdentity must be used within IdentityProvider");
  return ctx;
}

export function IdentityProvider({ children }: { children: ReactNode }) {
  const [identity, setIdentityState] = useState<StoredIdentity | null>(null);
  const [hydrated, setHydrated] = useState(false);

  useEffect(() => {
    setIdentityState(getIdentity());
    setHydrated(true);
  }, []);

  const setIdentity = useCallback((next: StoredIdentity) => {
    saveIdentity(next);
    setIdentityState(next);
  }, []);

  const logout = useCallback(() => {
    clearIdentity();
    setIdentityState(null);
  }, []);

  if (!hydrated) return null;

  return (
    <IdentityContext.Provider value={{ identity, setIdentity, logout }}>
      {!identity && <ParticipantSelector onSelect={setIdentity} />}
      {identity && children}
    </IdentityContext.Provider>
  );
}
