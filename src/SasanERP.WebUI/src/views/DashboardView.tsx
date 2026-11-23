import React, { useState, useEffect } from 'react';
import {
  IndianRupee,
  TrendingUp,
  PackageCheck,
  CreditCard,
  Banknote,
  QrCode,
  AlertTriangle,
  Flame,
  RotateCw,
  Plus,
  Box,
  Tags,
  Wallet,
  ShieldAlert
} from 'lucide-react';
import { KpiMetricCard } from '../components/dashboard/KpiMetricCard';
import { LowStockDeficitTable } from '../components/dashboard/LowStockDeficitTable';
import { AgingDeviceRadar } from '../components/dashboard/AgingDeviceRadar';
import { RecentSalesFeed } from '../components/dashboard/RecentSalesFeed';
import { DashboardTelemetry } from '../types/domain';

interface DashboardViewProps {
  onNavigate: (module: string) => void;
}

export const DashboardView: React.FC<DashboardViewProps> = ({ onNavigate }) => {
  const [timeframe, setTimeframe] = useState<'Today' | 'Yesterday' | 'ThisWeek' | 'ThisMonth'>('Today');
  const [loading, setLoading] = useState(false);
  const [activeRadarTab, setActiveRadarTab] = useState<'aging' | 'lowstock'>('lowstock');
  const [telemetry, setTelemetry] = useState<DashboardTelemetry>({
    todayRevenue: 0,
    todayProfit: 0,
    totalStockValuation: 406239,
    preOwnedValuation: 224997,
    bulkStockValuation: 181242,
    todayCashCollected: 0,
    todayUpiCollected: 0,
    customerCreditsReceivable: 0,
    agingDevicesCount: 0,
    lowStockCount: 2,
    invoicesCount: 0,
    agingDevices: [],
    lowStockItems: [
      {
        id: 1,
        name: 'Type-C 65W Fast Charger',
        sku: 'ACC-CHG-65W',
        category: 'Accessories',
        currentStock: 1,
        reorderLevel: 5,
        sellingPrice: 1499
      },
      {
        id: 2,
        name: 'Tempered Glass (S24 Ultra)',
        sku: 'ACC-TMP-S24U',
        category: 'Accessories',
        currentStock: 0,
        reorderLevel: 10,
        sellingPrice: 499
      }
    ],
    recentInvoices: []
  });

  const fetchTelemetry = async () => {
    setLoading(true);
    try {
      const res = await fetch(`http://localhost:5000/api/dashboard/telemetry?timeframe=${timeframe}`);
      if (res.ok) {
        const data = await res.json();
        setTelemetry(data);
        if (data.agingDevicesCount > 0 && data.lowStockCount === 0) {
          setActiveRadarTab('aging');
        }
      }
    } catch {
      // Graceful fallback to initial state in offline dev mode
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTelemetry();
    const interval = setInterval(fetchTelemetry, 15000);
    return () => clearInterval(interval);
  }, [timeframe]);

  return (
    <div className="p-6 space-y-6 max-w-[1920px] mx-auto">
      {/* Top Title & Header Telemetry */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-sasan-border pb-4">
        <div>
          <div className="flex items-center space-x-3">
            <h1 className="text-xl font-black tracking-tight text-sasan-text uppercase">
              Executive Dashboard & Live Telemetry
            </h1>
            <span className="inline-flex items-center space-x-1.5 px-2 py-0.5 rounded-full text-[10px] font-bold uppercase tracking-wider bg-emerald-950/80 border border-emerald-500/40 text-emerald-400">
              <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse" />
              <span>Online</span>
            </span>
          </div>
          <p className="text-xs text-sasan-muted mt-0.5">
            Theni Flagship Lounge • Rule 32(5) Margin Intelligence • Real-time Fiscal Telemetry
          </p>
        </div>

        {/* Timeframe Selector & Actions */}
        <div className="flex items-center space-x-2">
          <div className="flex items-center bg-sasan-surface border border-sasan-border rounded-lg p-1 space-x-1">
            {(
              [
                { id: 'Today', label: 'Today [F1]' },
                { id: 'Yesterday', label: 'Yesterday' },
                { id: 'ThisWeek', label: 'This Week' },
                { id: 'ThisMonth', label: 'This Month' }
              ] as const
            ).map((tab) => (
              <button
                key={tab.id}
                onClick={() => setTimeframe(tab.id)}
                className={`px-3 py-1 rounded text-xs font-semibold transition-all ${
                  timeframe === tab.id
                    ? 'bg-sasan-cyan text-slate-950 shadow-sm font-bold'
                    : 'text-sasan-muted hover:text-sasan-text hover:bg-slate-800/50'
                }`}
              >
                {tab.label}
              </button>
            ))}
          </div>

          <button
            onClick={fetchTelemetry}
            disabled={loading}
            className="p-2 rounded-lg bg-sasan-surface border border-sasan-border hover:bg-slate-800 text-sasan-muted hover:text-sasan-cyan transition-colors"
            title="Refresh Live Telemetry"
          >
            <RotateCw className={`w-4 h-4 ${loading ? 'animate-spin text-sasan-cyan' : ''}`} />
          </button>
        </div>
      </div>

      {/* Row 1: 4 Main KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <KpiMetricCard
          title="Today's Revenue"
          value={`₹${telemetry.todayRevenue.toLocaleString('en-IN')}`}
          subtitle={`${telemetry.invoicesCount} Invoices Billed`}
          icon={IndianRupee}
          colorVariant="cyan"
          badgeText="Realtime"
        />

        <KpiMetricCard
          title="Net Gross Profit"
          value={`₹${telemetry.todayProfit.toLocaleString('en-IN')}`}
          subtitle="Gross Margin on Items Sold"
          icon={TrendingUp}
          colorVariant="emerald"
          badgeText={telemetry.todayRevenue > 0 ? `${((telemetry.todayProfit / telemetry.todayRevenue) * 100).toFixed(1)}%` : '0.0%'}
        />

        <KpiMetricCard
          title="Total Inventory Valuation"
          value={`₹${telemetry.totalStockValuation.toLocaleString('en-IN')}`}
          subtitle="4 Pre-Owned, 23 Bulk Units"
          icon={PackageCheck}
          colorVariant="amber"
          badgeText="Asset"
        />

        <KpiMetricCard
          title="Khata Receivables"
          value={`₹${telemetry.customerCreditsReceivable.toLocaleString('en-IN')}`}
          subtitle="Outstanding Customer Balance"
          icon={CreditCard}
          colorVariant="rose"
          badgeText="Receivable"
        />
      </div>

      {/* Row 2: Secondary Mini Metric Strips */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-sasan-card border border-sasan-border rounded-xl p-3.5 flex items-center justify-between">
          <div>
            <div className="text-[10px] font-bold uppercase tracking-wider text-sasan-muted">
              Cash in Drawer (Till)
            </div>
            <div className="text-base font-black font-mono text-sasan-text mt-0.5">
              ₹{telemetry.todayCashCollected.toLocaleString('en-IN')}
            </div>
          </div>
          <div className="w-8 h-8 rounded-lg bg-emerald-950/40 border border-emerald-800/40 flex items-center justify-center text-emerald-400">
            <Banknote className="w-4 h-4" />
          </div>
        </div>

        <div className="bg-sasan-card border border-sasan-border rounded-xl p-3.5 flex items-center justify-between">
          <div>
            <div className="text-[10px] font-bold uppercase tracking-wider text-sasan-muted">
              UPI & Bank QR Inflow
            </div>
            <div className="text-base font-black font-mono text-sasan-text mt-0.5">
              ₹{telemetry.todayUpiCollected.toLocaleString('en-IN')}
            </div>
          </div>
          <div className="w-8 h-8 rounded-lg bg-cyan-950/40 border border-cyan-800/40 flex items-center justify-center text-cyan-400">
            <QrCode className="w-4 h-4" />
          </div>
        </div>

        <div className="bg-sasan-card border border-sasan-border rounded-xl p-3.5 flex items-center justify-between">
          <div>
            <div className="text-[10px] font-bold uppercase tracking-wider text-sasan-muted">
              Stock Aging (&gt;60 Days)
            </div>
            <div
              className={`text-base font-black font-mono mt-0.5 ${
                telemetry.agingDevicesCount > 0 ? 'text-amber-400' : 'text-emerald-400'
              }`}
            >
              {telemetry.agingDevicesCount} Devices Aged
            </div>
          </div>
          <div
            className={`w-8 h-8 rounded-lg flex items-center justify-center ${
              telemetry.agingDevicesCount > 0
                ? 'bg-amber-950/40 border border-amber-800/40 text-amber-400'
                : 'bg-emerald-950/40 border border-emerald-800/40 text-emerald-400'
            }`}
          >
            <Flame className="w-4 h-4" />
          </div>
        </div>

        <div className="bg-sasan-card border border-sasan-border rounded-xl p-3.5 flex items-center justify-between">
          <div>
            <div className="text-[10px] font-bold uppercase tracking-wider text-sasan-muted">
              Low Stock Reorder
            </div>
            <div
              className={`text-base font-black font-mono mt-0.5 ${
                telemetry.lowStockCount > 0 ? 'text-rose-400' : 'text-emerald-400'
              }`}
            >
              {telemetry.lowStockCount} Items Low
            </div>
          </div>
          <div
            className={`w-8 h-8 rounded-lg flex items-center justify-center ${
              telemetry.lowStockCount > 0
                ? 'bg-rose-950/40 border border-rose-800/40 text-rose-400'
                : 'bg-emerald-950/40 border border-emerald-800/40 text-emerald-400'
            }`}
          >
            <AlertTriangle className="w-4 h-4" />
          </div>
        </div>
      </div>

      {/* Quick Launch Action Bar */}
      <div className="bg-sasan-surface border border-sasan-border rounded-xl p-3 flex flex-wrap items-center justify-between gap-3">
        <span className="text-xs font-bold text-sasan-muted tracking-wider uppercase px-2">
          Quick Launch:
        </span>
        <div className="flex flex-wrap items-center gap-2">
          <button
            onClick={() => onNavigate('pos')}
            className="inline-flex items-center space-x-1.5 px-3 py-1.5 rounded-lg text-xs font-bold bg-sky-600 hover:bg-sky-500 text-white transition-colors shadow-sm"
          >
            <Plus className="w-3.5 h-3.5" />
            <span>[F2] Fresh POS Bill</span>
          </button>
          <button
            onClick={() => onNavigate('stock')}
            className="inline-flex items-center space-x-1.5 px-3 py-1.5 rounded-lg text-xs font-bold bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700 transition-colors"
          >
            <Box className="w-3.5 h-3.5 text-cyan-400" />
            <span>[F3] Stock Registry</span>
          </button>
          <button
            onClick={() => onNavigate('intake')}
            className="inline-flex items-center space-x-1.5 px-3 py-1.5 rounded-lg text-xs font-bold bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700 transition-colors"
          >
            <Tags className="w-3.5 h-3.5 text-amber-400" />
            <span>[F5] Intake & Barcode</span>
          </button>
          <button
            onClick={() => onNavigate('daybook')}
            className="inline-flex items-center space-x-1.5 px-3 py-1.5 rounded-lg text-xs font-bold bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700 transition-colors"
          >
            <Wallet className="w-3.5 h-3.5 text-emerald-400" />
            <span>[F6] Daybook Cash</span>
          </button>
          <button
            onClick={() => onNavigate('godmode')}
            className="inline-flex items-center space-x-1.5 px-3 py-1.5 rounded-lg text-xs font-bold bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700 transition-colors"
          >
            <ShieldAlert className="w-3.5 h-3.5 text-purple-400" />
            <span>[F12] God Mode & BI</span>
          </button>
        </div>
      </div>

      {/* Row 3: Dual Intelligence Feeds */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Left Column: Recent Sales Invoices */}
        <div className="bg-sasan-surface border border-sasan-border rounded-xl p-4 space-y-3">
          <div className="flex items-center justify-between pb-2 border-b border-sasan-border">
            <div className="flex items-center space-x-2">
              <h3 className="text-xs font-black uppercase tracking-wider text-sasan-text">
                Recent Sales Invoices & Audit Feed
              </h3>
              <span className="text-[10px] font-mono px-2 py-0.5 rounded-full bg-slate-800 text-cyan-400 border border-slate-700">
                {telemetry.recentInvoices.length} Records
              </span>
            </div>
            <button
              onClick={() => onNavigate('pos')}
              className="text-[11px] font-bold text-sasan-cyan hover:underline inline-flex items-center space-x-1"
            >
              <span>+ New Invoice [F2]</span>
            </button>
          </div>
          <RecentSalesFeed
            invoices={telemetry.recentInvoices}
            onLaunchPos={() => onNavigate('pos')}
            onInvoiceClick={(id) => console.log('Invoice clicked', id)}
          />
        </div>

        {/* Right Column: Tabbed Aging Radar & Low Stock Deficit */}
        <div className="bg-sasan-surface border border-sasan-border rounded-xl p-4 space-y-3">
          {/* Sub Tabs */}
          <div className="flex items-center justify-between pb-2 border-b border-sasan-border">
            <div className="flex items-center space-x-2">
              <button
                onClick={() => setActiveRadarTab('aging')}
                className={`inline-flex items-center space-x-1.5 px-3 py-1 rounded text-xs font-bold transition-all ${
                  activeRadarTab === 'aging'
                    ? 'bg-amber-500/20 text-amber-300 border border-amber-500/30'
                    : 'text-sasan-muted hover:text-sasan-text'
                }`}
              >
                <Flame className="w-3.5 h-3.5" />
                <span>Aging Radar</span>
                <span className="text-[10px] px-1.5 py-0.2 rounded-full bg-amber-950 border border-amber-800 text-amber-400">
                  {telemetry.agingDevicesCount}
                </span>
              </button>

              <button
                onClick={() => setActiveRadarTab('lowstock')}
                className={`inline-flex items-center space-x-1.5 px-3 py-1 rounded text-xs font-bold transition-all ${
                  activeRadarTab === 'lowstock'
                    ? 'bg-rose-500/20 text-rose-300 border border-rose-500/30'
                    : 'text-sasan-muted hover:text-sasan-text'
                }`}
              >
                <AlertTriangle className="w-3.5 h-3.5" />
                <span>Low Stock</span>
                <span className="text-[10px] px-1.5 py-0.2 rounded-full bg-rose-950 border border-rose-800 text-rose-400">
                  {telemetry.lowStockCount}
                </span>
              </button>
            </div>
          </div>

          {/* Active Content */}
          {activeRadarTab === 'aging' ? (
            <AgingDeviceRadar devices={telemetry.agingDevices} />
          ) : (
            <LowStockDeficitTable
              items={telemetry.lowStockItems}
              onNavigateToIntake={() => onNavigate('intake')}
            />
          )}
        </div>
      </div>
    </div>
  );
};
