import React, { useState, useEffect } from 'react';
import { 
  ShieldCheck, 
  User, 
  ZoomIn, 
  ZoomOut, 
  RotateCcw, 
  Maximize2, 
  Minimize2,
  Clock
} from 'lucide-react';

interface TopHeaderProps {
  activeModuleTitle: string;
  zoom: number;
  onZoomChange: (newZoom: number) => void;
}

export const TopHeader: React.FC<TopHeaderProps> = ({
  activeModuleTitle,
  zoom,
  onZoomChange
}) => {
  const [timeStr, setTimeStr] = useState<string>('');
  const [isFullscreen, setIsFullscreen] = useState(false);

  useEffect(() => {
    const updateTime = () => {
      const now = new Date();
      setTimeStr(now.toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: true }));
    };
    updateTime();
    const timer = setInterval(updateTime, 1000);
    return () => clearInterval(timer);
  }, []);

  const toggleFullscreen = () => {
    if (!document.fullscreenElement) {
      document.documentElement.requestFullscreen().catch(() => {});
      setIsFullscreen(true);
    } else {
      document.exitFullscreen().catch(() => {});
      setIsFullscreen(false);
    }
  };

  return (
    <header className="h-14 border-b border-sasan-border bg-sasan-surface px-4 flex items-center justify-between select-none">
      {/* Left: Brand Identity */}
      <div className="flex items-center space-x-3">
        <div className="w-9 h-9 rounded-full bg-sasan-card border border-sasan-cyan flex items-center justify-center p-0.5 overflow-hidden shadow-sm">
          <img src="/logo.png" alt="Sasan Logo" className="w-full h-full object-cover" />
        </div>
        <div>
          <div className="flex items-center space-x-2">
            <span className="font-black text-sm tracking-wide text-sasan-text uppercase">
              Sasan The Gadget Lounge
            </span>
            <span className="bg-sasan-gold/20 text-sasan-gold text-[10px] font-bold px-1.5 py-0.5 rounded border border-sasan-gold/40">
              THENI OUTLET
            </span>
            <span className="bg-sasan-card text-sasan-cyan text-[10px] font-mono font-semibold px-1.5 py-0.5 rounded border border-sasan-border hidden md:inline-block">
              GST: 33AAECS4821N1ZH
            </span>
          </div>
          <p className="text-[10px] text-sasan-muted truncate hidden sm:block">
            106/7, Karthik Mahall, Subban St, Theni - 625531
          </p>
        </div>
      </div>

      {/* Center: Active Module Badge */}
      <div className="hidden lg:flex items-center space-x-2 bg-sasan-card border border-sasan-border px-3 py-1 rounded-md">
        <span className="text-[11px] font-semibold text-sasan-muted uppercase">Active:</span>
        <span className="text-[11px] font-black text-sasan-cyan uppercase tracking-wider">{activeModuleTitle}</span>
      </div>

      {/* Right: Actions, Scale, Cashier, Time */}
      <div className="flex items-center space-x-2 text-xs">
        {/* Real-time Clock */}
        <div className="hidden xl:flex items-center space-x-1.5 bg-sasan-card border border-sasan-border px-2.5 py-1 rounded text-sasan-text font-mono text-[11px]">
          <Clock className="w-3.5 h-3.5 text-sasan-cyan" />
          <span>{timeStr}</span>
        </div>

        {/* Engine Status */}
        <div className="flex items-center space-x-1.5 bg-emerald-950/40 border border-emerald-600/40 text-emerald-400 px-2 py-1 rounded text-[11px] font-medium">
          <ShieldCheck className="w-3.5 h-3.5" />
          <span className="hidden md:inline">ACID Online</span>
        </div>

        {/* Scale Controls */}
        <div className="flex items-center bg-sasan-card border border-sasan-border rounded overflow-hidden">
          <button 
            onClick={() => onZoomChange(Math.max(0.75, Math.round((zoom - 0.05) * 100) / 100))}
            className="p-1 hover:bg-sasan-surface text-sasan-text"
            title="Zoom Out">
            <ZoomOut className="w-3.5 h-3.5" />
          </button>
          <span className="px-1.5 text-[10px] font-mono font-bold text-sasan-cyan w-11 text-center">
            {Math.round(zoom * 100)}%
          </span>
          <button 
            onClick={() => onZoomChange(Math.min(1.40, Math.round((zoom + 0.05) * 100) / 100))}
            className="p-1 hover:bg-sasan-surface text-sasan-text"
            title="Zoom In">
            <ZoomIn className="w-3.5 h-3.5" />
          </button>
          <button 
            onClick={() => onZoomChange(1.0)}
            className="px-1.5 py-0.5 border-l border-sasan-border hover:bg-sasan-surface text-[10px] font-bold text-sasan-muted"
            title="Reset Zoom">
            <RotateCcw className="w-3 h-3 inline" />
          </button>
        </div>

        {/* Cashier Badge */}
        <div className="hidden sm:flex items-center space-x-1.5 bg-sasan-card border border-sasan-border px-2.5 py-1 rounded">
          <User className="w-3.5 h-3.5 text-sasan-cyan" />
          <span className="font-semibold text-sasan-text text-[11px]">Karthik Raja</span>
        </div>

        {/* Fullscreen Toggle */}
        <button
          onClick={toggleFullscreen}
          className="p-1.5 bg-sasan-card border border-sasan-border rounded hover:border-sasan-cyan text-sasan-text"
          title="Toggle Fullscreen">
          {isFullscreen ? <Minimize2 className="w-3.5 h-3.5 text-sasan-gold" /> : <Maximize2 className="w-3.5 h-3.5 text-sasan-muted" />}
        </button>
      </div>
    </header>
  );
};
