import React, { useState } from 'react';
import {
  FileSpreadsheet,
  Download,
  Calendar,
  Sparkles,
  ShieldCheck,
  Printer,
  FileText
} from 'lucide-react';

export const GstReportsView: React.FC = () => {
  const [period, setPeriod] = useState<'ThisMonth' | 'LastMonth' | 'ThisQuarter'>('ThisMonth');

  const standardSales = {
    grossTaxable: 47457.63,
    cgst: 4271.19,
    sgst: 4271.19,
    totalTax: 8542.38,
    grossInvoiceValue: 56000.0
  };

  const marginSales = {
    totalSaleValue: 184000.0,
    totalPurchaseCost: 155000.0,
    grossMargin: 29000.0,
    marginTaxPayable: 4423.73,
    devicesSold: 3
  };

  const totalGstLiability = standardSales.totalTax + marginSales.marginTaxPayable;

  const handleDownloadPdf = (reportType: string) => {
    window.open(`http://localhost:5000/api/reports/${reportType}-pdf`, '_blank');
  };

  return (
    <div className="p-6 space-y-6 max-w-[1920px] mx-auto">
      {/* Header */}
      <div className="border-b border-sasan-border pb-4 flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center space-x-3">
            <h1 className="text-xl font-black tracking-tight text-sasan-text uppercase">
              GST Hub &amp; Rule 32(5) Margin Intelligence [F7]
            </h1>
            <span className="text-xs font-mono px-2.5 py-0.5 rounded-full bg-cyan-950/80 border border-cyan-800/40 text-cyan-400 font-bold">
              GSTIN: 33AAECS4821N1ZH (Tamil Nadu - 33)
            </span>
          </div>
          <p className="text-xs text-sasan-muted mt-0.5">
            Audit-grade bifurcation between Section 9 Standard Supply and Rule 32(5) Second-Hand Margin Tax.
          </p>
        </div>

        {/* Actions */}
        <div className="flex items-center space-x-2">
          <button
            onClick={() => handleDownloadPdf('sales-summary')}
            className="inline-flex items-center space-x-1.5 px-3 py-2 rounded-xl text-xs font-bold bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700 transition-colors"
          >
            <FileText className="w-3.5 h-3.5 text-cyan-400" />
            <span>Export Period Sales PDF</span>
          </button>
          <button
            onClick={() => handleDownloadPdf('stock-valuation')}
            className="inline-flex items-center space-x-1.5 px-3 py-2 rounded-xl text-xs font-bold bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700 transition-colors"
          >
            <Download className="w-3.5 h-3.5 text-emerald-400" />
            <span>Stock Valuation PDF</span>
          </button>
        </div>
      </div>

      {/* Tax Liability Top Banner */}
      <div className="bg-gradient-to-r from-cyan-950/50 via-slate-900 to-emerald-950/40 border border-sasan-border rounded-2xl p-6 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <span className="text-xs font-bold uppercase tracking-wider text-sasan-muted block">
            Net Estimated GST Liability for {period}
          </span>
          <div className="text-3xl font-black font-mono text-sasan-cyan tracking-tight mt-1">
            ₹{totalGstLiability.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
          </div>
          <p className="text-xs text-emerald-400 mt-1 font-semibold">
            Section 9 Standard (₹{standardSales.totalTax.toLocaleString('en-IN')}) + Rule 32(5) Margin (₹{marginSales.marginTaxPayable.toLocaleString('en-IN')})
          </p>
        </div>

        <div className="flex items-center space-x-2">
          <div className="flex items-center bg-sasan-card border border-sasan-border rounded-lg p-1 space-x-1">
            {(
              [
                { id: 'ThisMonth', label: 'This Month' },
                { id: 'LastMonth', label: 'Last Month' },
                { id: 'ThisQuarter', label: 'This Quarter' }
              ] as const
            ).map((tab) => (
              <button
                key={tab.id}
                onClick={() => setPeriod(tab.id)}
                className={`px-3 py-1 rounded text-xs font-semibold transition-all ${
                  period === tab.id
                    ? 'bg-sasan-cyan text-slate-950 font-bold'
                    : 'text-sasan-muted hover:text-sasan-text'
                }`}
              >
                {tab.label}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Dual Tax Tables */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Card 1: Section 9 Standard Supply */}
        <div className="bg-sasan-surface border border-sasan-border rounded-xl p-5 space-y-4">
          <div className="flex items-center justify-between pb-2 border-b border-sasan-border">
            <h3 className="text-xs font-bold uppercase tracking-wider text-sasan-text">
              1. Section 9 Standard Outward Supply (Accessories)
            </h3>
            <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-slate-800 text-slate-300">
              Full Value Taxable
            </span>
          </div>

          <div className="space-y-3 text-xs">
            <div className="flex justify-between py-1 border-b border-sasan-border">
              <span className="text-sasan-muted">Gross Taxable Value:</span>
              <span className="font-mono font-bold text-sasan-text">
                ₹{standardSales.grossTaxable.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
              </span>
            </div>
            <div className="flex justify-between py-1 border-b border-sasan-border">
              <span className="text-sasan-muted">CGST (9%):</span>
              <span className="font-mono font-bold text-cyan-400">
                ₹{standardSales.cgst.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
              </span>
            </div>
            <div className="flex justify-between py-1 border-b border-sasan-border">
              <span className="text-sasan-muted">SGST (9%):</span>
              <span className="font-mono font-bold text-cyan-400">
                ₹{standardSales.sgst.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
              </span>
            </div>
            <div className="flex justify-between py-2 bg-sasan-card px-3 rounded-lg">
              <span className="font-bold text-sasan-text">Total Section 9 GST:</span>
              <span className="font-mono font-black text-emerald-400 text-sm">
                ₹{standardSales.totalTax.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
              </span>
            </div>
          </div>
        </div>

        {/* Card 2: Rule 32(5) Pre-Owned Gadget Margin Scheme */}
        <div className="bg-sasan-surface border border-sasan-border rounded-xl p-5 space-y-4">
          <div className="flex items-center justify-between pb-2 border-b border-sasan-border">
            <h3 className="text-xs font-bold uppercase tracking-wider text-emerald-400 flex items-center space-x-1.5">
              <Sparkles className="w-3.5 h-3.5" />
              <span>2. Rule 32(5) Margin Scheme (Pre-Owned Phones)</span>
            </h3>
            <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-emerald-950 border border-emerald-800 text-emerald-300">
              Tax Only On Profit
            </span>
          </div>

          <div className="space-y-3 text-xs">
            <div className="flex justify-between py-1 border-b border-sasan-border">
              <span className="text-sasan-muted">Total Second-Hand Outflow (Sale):</span>
              <span className="font-mono font-bold text-sasan-text">
                ₹{marginSales.totalSaleValue.toLocaleString('en-IN')}
              </span>
            </div>
            <div className="flex justify-between py-1 border-b border-sasan-border">
              <span className="text-sasan-muted">Total Acquisition Cost (Purchase):</span>
              <span className="font-mono font-bold text-sasan-text">
                ₹{marginSales.totalPurchaseCost.toLocaleString('en-IN')}
              </span>
            </div>
            <div className="flex justify-between py-1 border-b border-sasan-border">
              <span className="text-sasan-muted">Gross Margin Realized:</span>
              <span className="font-mono font-bold text-amber-400">
                ₹{marginSales.grossMargin.toLocaleString('en-IN')}
              </span>
            </div>
            <div className="flex justify-between py-2 bg-emerald-950/40 border border-emerald-800/40 px-3 rounded-lg">
              <span className="font-bold text-emerald-300">Rule 32(5) GST Liability (18% on Margin):</span>
              <span className="font-mono font-black text-emerald-400 text-sm">
                ₹{marginSales.marginTaxPayable.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
