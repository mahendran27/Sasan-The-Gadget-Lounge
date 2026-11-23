import React from 'react';
import { ShoppingBag, ArrowUpRight, Receipt } from 'lucide-react';
import { RecentInvoice } from '../../types/domain';

interface RecentSalesFeedProps {
  invoices: RecentInvoice[];
  onInvoiceClick?: (invoiceId: number) => void;
  onLaunchPos: () => void;
}

export const RecentSalesFeed: React.FC<RecentSalesFeedProps> = ({
  invoices,
  onInvoiceClick,
  onLaunchPos
}) => {
  if (invoices.length === 0) {
    return (
      <div className="bg-sasan-surface border border-sasan-border rounded-xl p-8 flex flex-col items-center justify-center text-center">
        <div className="w-12 h-12 rounded-full bg-slate-800/80 border border-slate-700/60 flex items-center justify-center mb-3">
          <ShoppingBag className="w-6 h-6 text-sasan-muted" />
        </div>
        <h4 className="font-bold text-sm text-sasan-text">No Sales Billed in this Period</h4>
        <p className="text-xs text-sasan-muted mt-1 max-w-sm">
          Ready for retail checkout. Scan an IMEI barcode or press [F2] to launch POS and generate your first sale.
        </p>
        <button
          onClick={onLaunchPos}
          className="mt-4 inline-flex items-center space-x-1.5 bg-sasan-cyan hover:bg-cyan-400 text-slate-950 font-black px-4 py-2 rounded-lg text-xs transition-colors shadow-lg shadow-cyan-950/50"
        >
          <Receipt className="w-3.5 h-3.5" />
          <span>[F2] Launch POS Checkout</span>
        </button>
      </div>
    );
  }

  return (
    <div className="overflow-x-auto rounded-lg border border-sasan-border">
      <table className="w-full text-left text-xs">
        <thead className="bg-sasan-surface text-sasan-muted uppercase text-[10px] tracking-wider border-b border-sasan-border">
          <tr>
            <th className="py-2.5 px-3">Inv No</th>
            <th className="py-2.5 px-3">Customer</th>
            <th className="py-2.5 px-3">Time</th>
            <th className="py-2.5 px-3">Payment</th>
            <th className="py-2.5 px-3 text-right">Total</th>
            <th className="py-2.5 px-3 text-center w-10"></th>
          </tr>
        </thead>
        <tbody className="divide-y divide-sasan-border bg-sasan-card">
          {invoices.map((inv) => (
            <tr
              key={inv.id}
              onClick={() => onInvoiceClick && onInvoiceClick(inv.id)}
              className="hover:bg-slate-800/40 transition-colors cursor-pointer group"
            >
              <td className="py-2.5 px-3 font-mono font-bold text-sasan-cyan">
                {inv.invoiceNumber}
              </td>
              <td className="py-2.5 px-3 font-medium text-sasan-text">
                {inv.customerName || 'Walk-in Customer'}
              </td>
              <td className="py-2.5 px-3 font-mono text-[11px] text-sasan-muted">
                {new Date(inv.invoiceDate).toLocaleTimeString('en-IN', {
                  hour: '2-digit',
                  minute: '2-digit',
                  hour12: true
                })}
              </td>
              <td className="py-2.5 px-3">
                <span className="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider bg-slate-800 border border-slate-700 text-slate-300">
                  {inv.paymentModeSummary || 'Cash'}
                </span>
              </td>
              <td className="py-2.5 px-3 text-right font-mono font-bold text-emerald-400">
                ₹{inv.grandTotal.toLocaleString('en-IN')}
              </td>
              <td className="py-2.5 px-3 text-center">
                <ArrowUpRight className="w-3.5 h-3.5 text-slate-500 group-hover:text-sasan-cyan transition-colors" />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};
