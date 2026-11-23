import React, { useState } from 'react';
import { TopHeader } from './TopHeader';
import { SidebarRail, ModuleKey } from './SidebarRail';
import { useKeyboardShortcuts } from '../../hooks/useKeyboardShortcuts';

interface ShellProps {
  activeModule: ModuleKey;
  onSelectModule: (key: ModuleKey) => void;
  children: React.ReactNode;
}

const MODULE_TITLES: Record<ModuleKey, string> = {
  dashboard: 'Executive Dashboard & Live Telemetry',
  pos: 'High-Speed POS Billing & Checkout',
  stock: 'Stock Registry & Warranty Ledger',
  invoices: 'Invoice History & Document Auditing',
  intake: 'Device Intake & Barcode Printing',
  daybook: 'Daybook, Petty Cash & EBITDA Ledger',
  gst: 'GST Compliance & GSTR-1 Hub',
  wireless: 'Wireless Studio (NFC & UHF RFID)',
  settings: 'Store Profile & Statutory Settings',
  godmode: 'God Mode & Enterprise BI Controls',
};

export const Shell: React.FC<ShellProps> = ({
  activeModule,
  onSelectModule,
  children
}) => {
  const [zoom, setZoom] = useState<number>(1.0);

  // Global F1-F12 Hotkeys
  useKeyboardShortcuts({
    F1: () => onSelectModule('dashboard'),
    F2: () => onSelectModule('pos'),
    F3: () => onSelectModule('stock'),
    F4: () => onSelectModule('invoices'),
    F5: () => onSelectModule('intake'),
    F6: () => onSelectModule('daybook'),
    F7: () => onSelectModule('gst'),
    F8: () => onSelectModule('wireless'),
    F10: () => onSelectModule('settings'),
    F12: () => onSelectModule('godmode'),
  });

  return (
    <div className="flex flex-col h-screen w-screen bg-sasan-canvas text-sasan-text overflow-hidden">
      {/* Top Header */}
      <TopHeader
        activeModuleTitle={MODULE_TITLES[activeModule]}
        zoom={zoom}
        onZoomChange={setZoom}
      />

      {/* Main Body */}
      <div className="flex flex-1 overflow-hidden">
        {/* Sidebar Nav Rail */}
        <SidebarRail
          activeModule={activeModule}
          onSelectModule={onSelectModule}
        />

        {/* Content Workspace with Scale Transform */}
        <main className="flex-1 overflow-y-auto bg-sasan-canvas p-4 md:p-6">
          <div 
            style={{ 
              transform: `scale(${zoom})`, 
              transformOrigin: 'top left',
              width: `${100 / zoom}%`,
              transition: 'transform 0.15s ease-out'
            }}
          >
            {children}
          </div>
        </main>
      </div>

      {/* Bottom Status Bar */}
      <footer className="h-6 border-t border-sasan-border bg-sasan-surface px-3 flex items-center justify-between text-[10px] text-sasan-muted select-none">
        <div className="flex items-center space-x-3">
          <span className="flex items-center space-x-1">
            <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse"></span>
            <span>SQLite Engine: WAL Active</span>
          </span>
          <span>•</span>
          <span>Vector Print Engine: QuestPDF 2024</span>
          <span>•</span>
          <span>USB Scanner: Auto-Detecting</span>
        </div>
        <div className="flex items-center space-x-2 font-mono">
          <span>SasanERP Enterprise v2.5.0</span>
          <span>•</span>
          <span className="text-sasan-cyan">10/10 Certified</span>
        </div>
      </footer>
    </div>
  );
};
