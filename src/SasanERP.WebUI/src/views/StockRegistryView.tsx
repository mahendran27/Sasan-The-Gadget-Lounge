import React, { useState, useEffect } from 'react';
import {
  Smartphone,
  Search,
  Filter,
  Printer,
  Plus,
  Box,
  Flame,
  CheckCircle2,
  Clock,
  Battery
} from 'lucide-react';
import { BarcodeStickerPrintModal } from '../components/barcode/BarcodeStickerPrintModal';
import { SerializedUnit } from '../types/domain';

interface StockRegistryViewProps {
  onNavigateToIntake?: () => void;
}

export const StockRegistryView: React.FC<StockRegistryViewProps> = ({ onNavigateToIntake }) => {
  const [units, setUnits] = useState<SerializedUnit[]>([
    {
      id: 201,
      productId: 102,
      productName: 'Samsung Galaxy S24 (8GB / 256GB)',
      imei1: '358921104829104',
      serialNumber: 'R5CW109ABC',
      color: 'Onyx Black',
      storageCapacity: '256GB',
      condition: 'GradeA_Excellent',
      batteryHealthPercentage: 98,
      purchaseCost: 48000,
      sellingPrice: 56000,
      agingDays: 14,
      status: 'InStock'
    },
    {
      id: 202,
      productId: 103,
      productName: 'Apple iPhone 15 Pro (128GB)',
      imei1: '352901184910245',
      serialNumber: 'G6TW901XYZ',
      color: 'Natural Titanium',
      storageCapacity: '128GB',
      condition: 'LikeNew',
      batteryHealthPercentage: 100,
      purchaseCost: 72000,
      sellingPrice: 84999,
      agingDays: 8,
      status: 'InStock'
    },
    {
      id: 203,
      productId: 104,
      productName: 'OnePlus 12 (16GB / 512GB)',
      imei1: '351982001928374',
      serialNumber: 'OP129988',
      color: 'Emerald Green',
      storageCapacity: '512GB',
      condition: 'GradeB_Good',
      batteryHealthPercentage: 92,
      purchaseCost: 41000,
      sellingPrice: 48999,
      agingDays: 42,
      status: 'InStock'
    },
    {
      id: 204,
      productId: 105,
      productName: 'Google Pixel 8 (128GB)',
      imei1: '357891049182390',
      serialNumber: 'P89901AA',
      color: 'Hazel',
      storageCapacity: '128GB',
      condition: 'GradeA_Excellent',
      batteryHealthPercentage: 95,
      purchaseCost: 38000,
      sellingPrice: 45000,
      agingDays: 68,
      status: 'InStock'
    }
  ]);

  const [search, setSearch] = useState('');
  const [filterTab, setFilterTab] = useState<'All' | 'InStock' | 'Aging'>('All');
  const [selectedUnitForPrint, setSelectedUnitForPrint] = useState<SerializedUnit | undefined>(undefined);
  const [isPrintModalOpen, setIsPrintModalOpen] = useState(false);

  useEffect(() => {
    const fetchUnits = async () => {
      try {
        const res = await fetch('http://localhost:5000/api/inventory/units?inStockOnly=false');
        if (res.ok) {
          const data = await res.json();
          if (Array.isArray(data) && data.length > 0) {
            setUnits(data);
          }
        }
      } catch {
        // Fallback demo data
      }
    };
    fetchUnits();
  }, []);

  const filteredUnits = units.filter((u) => {
    if (filterTab === 'InStock' && u.status !== 'InStock') return false;
    if (filterTab === 'Aging' && u.agingDays < 60) return false;
    if (search.trim()) {
      const q = search.toLowerCase();
      return (
        u.productName.toLowerCase().includes(q) ||
        u.imei1.includes(q) ||
        (u.serialNumber && u.serialNumber.toLowerCase().includes(q))
      );
    }
    return true;
  });

  const totalValuation = filteredUnits.reduce((acc, u) => acc + u.sellingPrice, 0);

  const handleOpenPrint = (unit: SerializedUnit) => {
    setSelectedUnitForPrint(unit);
    setIsPrintModalOpen(true);
  };

  return (
    <div className="p-6 space-y-6 max-w-[1920px] mx-auto">
      {/* Header Bar */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-sasan-border pb-4">
        <div>
          <div className="flex items-center space-x-3">
            <h1 className="text-xl font-black tracking-tight text-sasan-text uppercase">
              Stock Registry &amp; Serialized Device Ledger
            </h1>
            <span className="text-xs font-mono px-2.5 py-0.5 rounded-full bg-cyan-950/80 border border-cyan-800/40 text-cyan-400 font-bold">
              {units.length} Serialized Units Active
            </span>
          </div>
          <p className="text-xs text-sasan-muted mt-0.5">
            Individual IMEI Tracking • Rule 32(5) Valuation • Battery &amp; Grade Telemetry
          </p>
        </div>

        {/* Action Controls */}
        <div className="flex items-center space-x-3">
          <button
            onClick={onNavigateToIntake}
            className="inline-flex items-center space-x-1.5 px-4 py-2 rounded-xl text-xs font-black bg-sasan-cyan hover:bg-cyan-400 text-slate-950 transition-colors shadow-lg shadow-cyan-950/50"
          >
            <Plus className="w-4 h-4" />
            <span>+ Intake Device [F5]</span>
          </button>
        </div>
      </div>

      {/* Filter and Search Bar */}
      <div className="flex flex-col sm:flex-row items-center justify-between gap-4 bg-sasan-surface border border-sasan-border rounded-xl p-3">
        <div className="relative w-full sm:w-96">
          <Search className="w-4 h-4 absolute left-3 top-3 text-sasan-muted" />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search by IMEI, Serial, Model, Brand..."
            className="w-full bg-sasan-card border border-sasan-border rounded-lg pl-9 pr-3 py-2 text-xs text-sasan-text font-mono placeholder:text-sasan-muted focus:outline-none focus:border-sasan-cyan"
          />
        </div>

        {/* Filter Tabs */}
        <div className="flex items-center space-x-2">
          <div className="flex items-center bg-sasan-card border border-sasan-border rounded-lg p-1 space-x-1">
            {(
              [
                { id: 'All', label: 'All Registered' },
                { id: 'InStock', label: 'In Stock Only' },
                { id: 'Aging', label: 'Aging (>60 Days)' }
              ] as const
            ).map((tab) => (
              <button
                key={tab.id}
                onClick={() => setFilterTab(tab.id)}
                className={`px-3 py-1 rounded text-xs font-semibold transition-all ${
                  filterTab === tab.id
                    ? 'bg-sasan-cyan text-slate-950 font-bold'
                    : 'text-sasan-muted hover:text-sasan-text'
                }`}
              >
                {tab.label}
              </button>
            ))}
          </div>

          <div className="text-right pl-2 hidden lg:block">
            <span className="text-[10px] uppercase font-bold text-sasan-muted block">
              Filtered Value
            </span>
            <span className="font-mono font-bold text-xs text-emerald-400">
              ₹{totalValuation.toLocaleString('en-IN')}
            </span>
          </div>
        </div>
      </div>

      {/* High Contrast Registry Table */}
      <div className="overflow-x-auto rounded-xl border border-sasan-border bg-sasan-surface">
        <table className="w-full text-left text-xs">
          <thead className="bg-sasan-card text-sasan-muted uppercase text-[10px] tracking-wider border-b border-sasan-border">
            <tr>
              <th className="py-3 px-3">Device Model &amp; Specs</th>
              <th className="py-3 px-3">IMEI 1 &amp; Serial</th>
              <th className="py-3 px-3">Condition Grade</th>
              <th className="py-3 px-3">Battery</th>
              <th className="py-3 px-3">Aging</th>
              <th className="py-3 px-3 text-right">Cost (₹)</th>
              <th className="py-3 px-3 text-right">Tagged (₹)</th>
              <th className="py-3 px-3 text-center">Label Action</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-sasan-border">
            {filteredUnits.map((u) => {
              const isAging = u.agingDays >= 60;
              return (
                <tr key={u.id} className="hover:bg-slate-800/30 transition-colors">
                  <td className="py-3 px-3">
                    <div className="font-bold text-sasan-text flex items-center space-x-1.5">
                      <Smartphone className="w-3.5 h-3.5 text-sasan-cyan flex-shrink-0" />
                      <span>{u.productName}</span>
                    </div>
                    <div className="text-[11px] text-sasan-muted font-mono mt-0.5">
                      {u.storageCapacity} • {u.color}
                    </div>
                  </td>

                  <td className="py-3 px-3">
                    <div className="font-mono font-bold text-cyan-300">{u.imei1}</div>
                    {u.serialNumber && (
                      <div className="font-mono text-[10px] text-sasan-muted">
                        S/N: {u.serialNumber}
                      </div>
                    )}
                  </td>

                  <td className="py-3 px-3">
                    <span className="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-bold uppercase bg-slate-800 border border-slate-700 text-amber-300">
                      {u.condition.replace(/_/g, ' ')}
                    </span>
                  </td>

                  <td className="py-3 px-3">
                    <div className="flex items-center space-x-1 font-mono text-[11px] font-bold text-emerald-400">
                      <Battery className="w-3.5 h-3.5" />
                      <span>{u.batteryHealthPercentage || 98}%</span>
                    </div>
                  </td>

                  <td className="py-3 px-3">
                    <span
                      className={`inline-flex items-center space-x-1 px-2 py-0.5 rounded text-[10px] font-bold border ${
                        isAging
                          ? 'bg-rose-950/60 border-rose-800/50 text-rose-300'
                          : 'bg-emerald-950/60 border-emerald-800/50 text-emerald-300'
                      }`}
                    >
                      {isAging ? <Flame className="w-3 h-3 text-rose-400" /> : <Clock className="w-3 h-3" />}
                      <span>{u.agingDays}d</span>
                    </span>
                  </td>

                  <td className="py-3 px-3 text-right font-mono font-semibold text-sasan-muted">
                    ₹{u.purchaseCost.toLocaleString('en-IN')}
                  </td>

                  <td className="py-3 px-3 text-right font-mono font-bold text-sm text-emerald-400">
                    ₹{u.sellingPrice.toLocaleString('en-IN')}
                  </td>

                  <td className="py-3 px-3 text-center">
                    <button
                      onClick={() => handleOpenPrint(u)}
                      className="inline-flex items-center space-x-1 px-2.5 py-1 rounded bg-slate-800 hover:bg-slate-700 border border-slate-700 text-sasan-cyan text-[11px] font-bold transition-colors"
                      title="Print 7-Style TSPL Barcode Label"
                    >
                      <Printer className="w-3.5 h-3.5" />
                      <span>Sticker [F5]</span>
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* Barcode Sticker Modal */}
      <BarcodeStickerPrintModal
        isOpen={isPrintModalOpen}
        onClose={() => setIsPrintModalOpen(false)}
        unit={selectedUnitForPrint}
      />
    </div>
  );
};
