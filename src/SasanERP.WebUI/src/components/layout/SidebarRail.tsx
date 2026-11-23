import React from 'react';
import { 
  LayoutDashboard, 
  ShoppingCart, 
  Package, 
  Receipt, 
  Tag, 
  BookOpen, 
  FileText, 
  Radio, 
  Settings, 
  Crown,
  Plus
} from 'lucide-react';

export type ModuleKey = 
  | 'dashboard'
  | 'pos'
  | 'stock'
  | 'invoices'
  | 'intake'
  | 'daybook'
  | 'gst'
  | 'wireless'
  | 'settings'
  | 'godmode';

interface NavItem {
  key: ModuleKey;
  label: string;
  hotkey: string;
  icon: React.ComponentType<{ className?: string }>;
}

const NAV_ITEMS: NavItem[] = [
  { key: 'dashboard', label: 'Dashboard', hotkey: 'F1', icon: LayoutDashboard },
  { key: 'pos', label: 'POS Billing', hotkey: 'F2', icon: ShoppingCart },
  { key: 'stock', label: 'Stock Registry', hotkey: 'F3', icon: Package },
  { key: 'invoices', label: 'Invoices & Bills', hotkey: 'F4', icon: Receipt },
  { key: 'intake', label: 'Intake & Stickers', hotkey: 'F5', icon: Tag },
  { key: 'daybook', label: 'Daybook & Ledgers', hotkey: 'F6', icon: BookOpen },
  { key: 'gst', label: 'GST & GSTR-1 Hub', hotkey: 'F7', icon: FileText },
  { key: 'wireless', label: 'NFC & UHF RFID', hotkey: 'F8', icon: Radio },
  { key: 'settings', label: 'Store Settings', hotkey: 'F10', icon: Settings },
  { key: 'godmode', label: 'God Mode & BI', hotkey: 'F12', icon: Crown },
];

interface SidebarRailProps {
  activeModule: ModuleKey;
  onSelectModule: (key: ModuleKey) => void;
}

export const SidebarRail: React.FC<SidebarRailProps> = ({
  activeModule,
  onSelectModule
}) => {
  return (
    <aside className="w-56 border-r border-sasan-border bg-sasan-surface flex flex-col justify-between py-3 select-none shrink-0">
      <div className="space-y-1 px-2">
        <p className="px-3 pb-2 text-[10px] font-bold text-sasan-muted uppercase tracking-wider">
          ERP Modules
        </p>
        {NAV_ITEMS.map((item) => {
          const Icon = item.icon;
          const isActive = activeModule === item.key;
          return (
            <button
              key={item.key}
              onClick={() => onSelectModule(item.key)}
              className={`w-full flex items-center justify-between px-3 py-2 rounded-lg text-xs font-semibold transition-all ${
                isActive
                  ? 'bg-gradient-to-r from-cyan-950/60 to-sasan-card text-sasan-cyan border-l-2 border-sasan-cyan shadow-inner'
                  : 'text-sasan-text/80 hover:bg-sasan-card hover:text-sasan-text border-l-2 border-transparent'
              }`}
            >
              <div className="flex items-center space-x-2.5">
                <Icon className={`w-4 h-4 ${isActive ? 'text-sasan-cyan' : 'text-sasan-muted'}`} />
                <span>{item.label}</span>
              </div>
              <span className={`text-[10px] font-mono px-1.5 py-0.5 rounded font-bold ${
                isActive 
                  ? 'bg-sasan-cyan/20 text-sasan-cyan' 
                  : 'bg-sasan-card text-sasan-muted'
              }`}>
                [{item.hotkey}]
              </span>
            </button>
          );
        })}
      </div>

      {/* Bottom Quick Billing Action Bar */}
      <div className="p-3 border-t border-sasan-border space-y-2">
        <button
          onClick={() => onSelectModule('pos')}
          className="w-full flex items-center justify-center space-x-1.5 bg-sasan-cyan hover:bg-cyan-400 text-[#080E1A] py-2 px-3 rounded-lg text-xs font-black shadow-lg shadow-cyan-950/50 transition-all"
        >
          <Plus className="w-4 h-4" />
          <span>[F2] Fresh POS Bill</span>
        </button>
        <p className="text-[10px] text-center text-sasan-muted">
          Press <kbd className="font-mono text-sasan-cyan">F1-F12</kbd> anytime
        </p>
      </div>
    </aside>
  );
};
