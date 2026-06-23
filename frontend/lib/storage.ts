import type { StoredIdentity } from "@/types";

const IDENTITY_KEY = "bolao_identity";

export function getIdentity(): StoredIdentity | null {
  if (typeof window === "undefined") return null;
  const raw = localStorage.getItem(IDENTITY_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as StoredIdentity;
  } catch {
    return null;
  }
}

export function saveIdentity(identity: StoredIdentity): void {
  localStorage.setItem(IDENTITY_KEY, JSON.stringify(identity));
}

export function clearIdentity(): void {
  localStorage.removeItem(IDENTITY_KEY);
}
