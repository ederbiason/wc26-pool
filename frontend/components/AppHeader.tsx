"use client";

import { Settings } from "lucide-react";
import { useState } from "react";
import { SettingsDrawer } from "@/components/SettingsDrawer";

export function AppHeader() {
  const [open, setOpen] = useState(false);

  return (
    <>
      <header className="sticky top-0 z-30 bg-brand-green/95 backdrop-blur-md border-b border-[#1E4A32]">
        <div className="flex items-center justify-between px-4 h-14">
          <div className="flex items-center gap-2">
            <span className="text-2xl leading-none">🏆</span>
            <span className="font-display text-brand-gold text-2xl tracking-wider leading-none">
              BOLÃO DOS NOJEIRAS
            </span>
          </div>
          <button
            id="settings-btn"
            onClick={() => setOpen(true)}
            aria-label="Configurações"
            className="touch-target flex items-center justify-center text-[#86B59A] hover:text-[#E8F5E9] transition-colors"
          >
            <Settings size={22} />
          </button>
        </div>
      </header>
      <SettingsDrawer open={open} onClose={() => setOpen(false)} />
    </>
  );
}
