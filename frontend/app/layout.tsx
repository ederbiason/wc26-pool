import type { Metadata, Viewport } from "next";
import { Inter, Bebas_Neue } from "next/font/google";
import "./globals.css";
import { Toaster } from "@/components/ui/sonner";
import { IdentityProvider } from "@/components/IdentityProvider";
import { AppNav } from "@/components/AppNav";

const inter = Inter({
  subsets: ["latin"],
  variable: "--font-inter",
  display: "swap",
});

const bebasNeue = Bebas_Neue({
  subsets: ["latin"],
  weight: "400",
  variable: "--font-bebas",
  display: "swap",
});

export const metadata: Metadata = {
  title: "Bolão dos Nojeiras — Copa 2026",
  description:
    "Bolão da Copa do Mundo 2026 do grupo dos Nojeiras. Faça seus palpites e acompanhe o ranking.",
  keywords: ["bolão", "copa do mundo", "2026", "palpites", "futebol"],
};

export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
  maximumScale: 1,
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="pt-BR" className={`${inter.variable} ${bebasNeue.variable}`}>
      <body>
        <IdentityProvider>
          <div className="min-h-screen flex flex-col max-w-lg mx-auto pb-20">
            {children}
          </div>
          <AppNav />
        </IdentityProvider>
        <Toaster richColors position="top-center" />
      </body>
    </html>
  );
}
