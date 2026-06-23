"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { Home, CalendarDays, Trophy } from "lucide-react";

const NAV_ITEMS = [
  { href: "/", label: "Hoje", icon: Home },
  { href: "/calendar", label: "Agenda", icon: CalendarDays },
  { href: "/ranking", label: "Ranking", icon: Trophy },
];

export function AppNav() {
  const pathname = usePathname();

  return (
    <nav className="fixed bottom-0 left-0 right-0 z-40 flex justify-center">
      <div className="w-full max-w-lg bg-[#0A2E1E]/95 backdrop-blur-md border-t border-[#1E4A32]">
        <div className="flex justify-around items-center h-16 px-4">
          {NAV_ITEMS.map(({ href, label, icon: Icon }) => {
            const active = pathname === href;
            return (
              <Link
                key={href}
                href={href}
                id={`nav-${label.toLowerCase()}`}
                className={`flex flex-col items-center gap-0.5 touch-target justify-center px-6 transition-colors duration-150 ${
                  active
                    ? "text-[#FFD600]"
                    : "text-[#86B59A] hover:text-[#E8F5E9]"
                }`}
              >
                <Icon
                  size={22}
                  strokeWidth={active ? 2.5 : 1.75}
                  className="transition-all duration-150"
                />
                <span className="text-[10px] font-semibold uppercase tracking-widest leading-none">
                  {label}
                </span>
                {active && (
                  <span className="absolute -top-px w-8 h-0.5 bg-[#FFD600] rounded-full" />
                )}
              </Link>
            );
          })}
        </div>
      </div>
    </nav>
  );
}
