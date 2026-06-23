export function MatchCardSkeleton() {
  return (
    <div className="bg-[#112B1E] rounded-2xl border border-[#1E4A32] overflow-hidden animate-pulse">
      <div className="flex items-center justify-between px-4 pt-3 pb-2">
        <div className="h-5 w-20 rounded-full bg-[#1A3D2B]" />
        <div className="h-4 w-12 rounded bg-[#1A3D2B]" />
      </div>
      <div className="flex items-center justify-between px-4 pb-4">
        <div className="flex flex-col items-center gap-2 w-[90px]">
          <div className="w-12 h-12 rounded-xl bg-[#1A3D2B]" />
          <div className="h-3 w-16 rounded bg-[#1A3D2B]" />
        </div>
        <div className="flex flex-col items-center gap-1">
          <div className="h-10 w-20 rounded bg-[#1A3D2B]" />
        </div>
        <div className="flex flex-col items-center gap-2 w-[90px]">
          <div className="w-12 h-12 rounded-xl bg-[#1A3D2B]" />
          <div className="h-3 w-16 rounded bg-[#1A3D2B]" />
        </div>
      </div>
    </div>
  );
}
