import React from 'react';
import { AlertTriangle, CheckCircle2, ArrowRight } from 'lucide-react';

interface LowStockItem {
  id: number;
  name: string;
  sku: string;
  category: string;
  currentStock: number;
  reorderLevel: number;
  sellingPrice: number;
}

interface LowStockDeficitTableProps {
  items: LowStockItem[];
  onNavigateToIntake: () => void;
}

export const LowStockDeficitTable: React.FC<LowStockDeficitTableProps> = ({
  items,
  onNavigateToIntake
}) => {
  if (items.length === 0) {
    return (
      <div className="bg-emerald-950/20 border border-emerald-600/30 rounded-xl p-8 flex flex-col items-center justify-center text-center">
        <CheckCircle2 className="w-10 h-10 text-emerald-400 mb-2" />
        <h4 className="font-bold text-sm text-emerald-300">All Items Above Reorder Threshold</h4>
        <p className="text-xs text-emerald-500/80 mt-1 max-w-sm">
          No stock deficits detected across retail accessories or pre-owned inventory.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <div className="overflow-x-auto rounded-lg border border-sasan-border">
        <table className="w-full text-left text-xs">
          <thead className="bg-sasan-surface text-sasan-muted uppercase text-[10px] tracking-wider border-b border-sasan-border">
            <tr>
              <th className="py-2.5 px-3">Item Name</th>
              <th className="py-2.5 px-3">SKU</th>
              <th className="py-2.5 px-3">Stock</th>
              <th className="py-2.5 px-3">Reorder</th>
              <th className="py-2.5 px-3 text-right">Price</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-sasan-border bg-sasan-card">
            {items.map((item) => (
              <tr key={item.id} className="hover:bg-slate-800/40 transition-colors">
                <td className="py-2.5 px-3 font-semibold text-sasan-text">
                  {item.name}
                </td>
                <td className="py-2.5 px-3 font-mono text-[11px] text-cyan-400">
                  <span className="bg-cyan-950/50 border border-cyan-800/40 px-1.5 py-0.5 rounded">
                    {item.sku}
                  </span>
                </td>
                <td className="py-2.5 px-3">
                  <span className="bg-rose-950/60 border border-rose-800/50 text-rose-300 font-bold px-2 py-0.5 rounded text-[11px] inline-flex items-center space-x-1">
                    <AlertTriangle className="w-3 h-3 text-rose-400" />
                    <span>{item.currentStock} Left</span>
                  </span>
                </td>
                <td className="py-2.5 px-3 text-sasan-muted font-mono">
                  Min: {item.reorderLevel}
                </td>
                <td className="py-2.5 px-3 text-right font-mono font-bold text-emerald-400">
                  ₹{item.sellingPrice.toLocaleString('en-IN')}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="flex items-center justify-between bg-sasan-card border border-sasan-border px-3 py-2 rounded-lg text-xs">
        <span className="text-rose-300 text-[11px] font-medium flex items-center space-x-1.5">
          <AlertTriangle className="w-3.5 h-3.5 text-rose-400" />
          <span>Deficit detected in retail stock</span>
        </span>
        <button
          onClick={onNavigateToIntake}
          className="inline-flex items-center space-x-1 bg-sky-600 hover:bg-sky-500 text-white font-bold px-2.5 py-1 rounded text-[11px] transition-colors"
        >
          <span>Restock via Intake [F5]</span>
          <ArrowRight className="w-3 h-3" />
        </button>
      </div>
    </div>
  );
};
