import React, { useState } from 'react';
import {
  X,
  CreditCard,
  Banknote,
  QrCode,
  Smartphone,
  CheckCircle2,
  AlertCircle,
  Printer
} from 'lucide-react';
import { Customer } from '../../types/domain';

interface SplitPaymentModalProps {
  isOpen: boolean;
  onClose: () => void;
  grandTotal: number;
  customer?: Customer;
  onConfirmCheckout: (payments: Array<{ mode: string; amount: number; reference?: string }>) => void;
  isProcessing: boolean;
}

export const SplitPaymentModal: React.FC<SplitPaymentModalProps> = ({
  isOpen,
  onClose,
  grandTotal,
  customer,
  onConfirmCheckout,
  isProcessing
}) => {
  const [cashAmount, setCashAmount] = useState<number>(grandTotal);
  const [upiAmount, setUpiAmount] = useState<number>(0);
  const [cardAmount, setCardAmount] = useState<number>(0);
  const [exchangeAmount, setExchangeAmount] = useState<number>(0);
  const [creditAmount, setCreditAmount] = useState<number>(0);

  if (!isOpen) return null;

  const totalTendered = cashAmount + upiAmount + cardAmount + exchangeAmount + creditAmount;
  const balanceRemaining = grandTotal - totalTendered;
  const isSettled = Math.abs(balanceRemaining) < 0.01;

  const handleQuickPay = (type: 'cash' | 'upi' | 'split') => {
    if (type === 'cash') {
      setCashAmount(grandTotal);
      setUpiAmount(0);
      setCardAmount(0);
      setExchangeAmount(0);
      setCreditAmount(0);
    } else if (type === 'upi') {
      setCashAmount(0);
      setUpiAmount(grandTotal);
      setCardAmount(0);
      setExchangeAmount(0);
      setCreditAmount(0);
    } else if (type === 'split') {
      const half = Math.floor(grandTotal / 2);
      setCashAmount(half);
      setUpiAmount(grandTotal - half);
      setCardAmount(0);
      setExchangeAmount(0);
      setCreditAmount(0);
    }
  };

  const handleCheckout = () => {
    if (!isSettled) return;
    const payments: Array<{ mode: string; amount: number; reference?: string }> = [];
    if (cashAmount > 0) payments.push({ mode: 'Cash', amount: cashAmount });
    if (upiAmount > 0) payments.push({ mode: 'UPI', amount: upiAmount, reference: 'UPI-STORE-QR' });
    if (cardAmount > 0) payments.push({ mode: 'Card', amount: cardAmount });
    if (exchangeAmount > 0) payments.push({ mode: 'OldPhoneExchange', amount: exchangeAmount });
    if (creditAmount > 0) payments.push({ mode: 'CreditKhata', amount: creditAmount });
    onConfirmCheckout(payments);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 backdrop-blur-sm p-4">
      <div className="bg-sasan-surface border border-sasan-border rounded-2xl w-full max-w-2xl shadow-2xl overflow-hidden flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-sasan-border bg-sasan-card">
          <div className="flex items-center space-x-2">
            <CreditCard className="w-5 h-5 text-sasan-cyan" />
            <h3 className="font-bold text-sm text-sasan-text uppercase tracking-wider">
              Split Tender & Checkout Engine
            </h3>
          </div>
          <button
            onClick={onClose}
            className="p-1 rounded-lg text-sasan-muted hover:text-sasan-text hover:bg-slate-800 transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Amount Display */}
        <div className="p-6 bg-slate-950/60 border-b border-sasan-border flex items-center justify-between">
          <div>
            <span className="text-xs uppercase font-bold text-sasan-muted tracking-wider">
              Payable Grand Total
            </span>
            <div className="text-3xl font-black font-mono text-sasan-cyan tracking-tight mt-0.5">
              ₹{grandTotal.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
            </div>
            {customer && (
              <p className="text-xs text-sasan-muted mt-1">
                Customer: <span className="text-sasan-text font-semibold">{customer.fullName}</span> ({customer.phoneNumber})
              </p>
            )}
          </div>

          {/* Quick Pay Buttons */}
          <div className="flex flex-col space-y-1.5">
            <span className="text-[10px] uppercase font-bold text-sasan-muted text-right">
              Quick Shortcuts
            </span>
            <div className="flex space-x-1.5">
              <button
                type="button"
                onClick={() => handleQuickPay('cash')}
                className="px-2.5 py-1 text-xs font-bold rounded bg-slate-800 border border-slate-700 text-slate-200 hover:border-sasan-cyan transition-colors"
              >
                Exact Cash
              </button>
              <button
                type="button"
                onClick={() => handleQuickPay('upi')}
                className="px-2.5 py-1 text-xs font-bold rounded bg-slate-800 border border-slate-700 text-cyan-400 hover:border-sasan-cyan transition-colors"
              >
                Full UPI
              </button>
              <button
                type="button"
                onClick={() => handleQuickPay('split')}
                className="px-2.5 py-1 text-xs font-bold rounded bg-slate-800 border border-slate-700 text-amber-400 hover:border-sasan-gold transition-colors"
              >
                50:50 Split
              </button>
            </div>
          </div>
        </div>

        {/* Multi-tender Form */}
        <div className="p-6 space-y-4">
          <div className="grid grid-cols-2 gap-4">
            {/* Cash */}
            <div className="bg-sasan-card p-3 rounded-xl border border-sasan-border">
              <label className="text-xs font-bold text-sasan-muted uppercase flex items-center space-x-1.5 mb-1.5">
                <Banknote className="w-3.5 h-3.5 text-emerald-400" />
                <span>Cash Payment (Till)</span>
              </label>
              <div className="relative">
                <span className="absolute left-3 top-2.5 text-sm font-bold text-sasan-muted">₹</span>
                <input
                  type="number"
                  value={cashAmount || ''}
                  onChange={(e) => setCashAmount(parseFloat(e.target.value) || 0)}
                  className="w-full bg-slate-900 border border-sasan-border rounded-lg py-2 pl-8 pr-3 text-sm font-mono font-bold text-sasan-text focus:outline-none focus:border-sasan-cyan"
                  placeholder="0"
                />
              </div>
            </div>

            {/* UPI QR */}
            <div className="bg-sasan-card p-3 rounded-xl border border-sasan-border">
              <label className="text-xs font-bold text-sasan-muted uppercase flex items-center space-x-1.5 mb-1.5">
                <QrCode className="w-3.5 h-3.5 text-sasan-cyan" />
                <span>UPI / QR Transfer</span>
              </label>
              <div className="relative">
                <span className="absolute left-3 top-2.5 text-sm font-bold text-sasan-muted">₹</span>
                <input
                  type="number"
                  value={upiAmount || ''}
                  onChange={(e) => setUpiAmount(parseFloat(e.target.value) || 0)}
                  className="w-full bg-slate-900 border border-sasan-border rounded-lg py-2 pl-8 pr-3 text-sm font-mono font-bold text-sasan-cyan focus:outline-none focus:border-sasan-cyan"
                  placeholder="0"
                />
              </div>
            </div>

            {/* Card / POS Machine */}
            <div className="bg-sasan-card p-3 rounded-xl border border-sasan-border">
              <label className="text-xs font-bold text-sasan-muted uppercase flex items-center space-x-1.5 mb-1.5">
                <CreditCard className="w-3.5 h-3.5 text-sky-400" />
                <span>Card (Debit/Credit EDC)</span>
              </label>
              <div className="relative">
                <span className="absolute left-3 top-2.5 text-sm font-bold text-sasan-muted">₹</span>
                <input
                  type="number"
                  value={cardAmount || ''}
                  onChange={(e) => setCardAmount(parseFloat(e.target.value) || 0)}
                  className="w-full bg-slate-900 border border-sasan-border rounded-lg py-2 pl-8 pr-3 text-sm font-mono font-bold text-sasan-text focus:outline-none focus:border-sasan-cyan"
                  placeholder="0"
                />
              </div>
            </div>

            {/* Old Phone Exchange */}
            <div className="bg-sasan-card p-3 rounded-xl border border-sasan-border">
              <label className="text-xs font-bold text-sasan-muted uppercase flex items-center space-x-1.5 mb-1.5">
                <Smartphone className="w-3.5 h-3.5 text-amber-400" />
                <span>Old Phone Exchange Value</span>
              </label>
              <div className="relative">
                <span className="absolute left-3 top-2.5 text-sm font-bold text-sasan-muted">₹</span>
                <input
                  type="number"
                  value={exchangeAmount || ''}
                  onChange={(e) => setExchangeAmount(parseFloat(e.target.value) || 0)}
                  className="w-full bg-slate-900 border border-sasan-border rounded-lg py-2 pl-8 pr-3 text-sm font-mono font-bold text-amber-400 focus:outline-none focus:border-sasan-cyan"
                  placeholder="0"
                />
              </div>
            </div>
          </div>

          {/* Balance Check */}
          <div className="flex items-center justify-between p-3.5 rounded-xl border bg-slate-900/80">
            <div className="flex items-center space-x-2">
              {isSettled ? (
                <CheckCircle2 className="w-5 h-5 text-emerald-400" />
              ) : (
                <AlertCircle className="w-5 h-5 text-rose-400" />
              )}
              <div>
                <div className="text-xs font-bold text-sasan-text">
                  {isSettled ? 'Tender Fully Settled' : 'Balance Remaining'}
                </div>
                <div className="text-[11px] text-sasan-muted">
                  Total Tendered: ₹{totalTendered.toLocaleString('en-IN')}
                </div>
              </div>
            </div>
            <div
              className={`text-xl font-mono font-black ${
                isSettled ? 'text-emerald-400' : 'text-rose-400'
              }`}
            >
              ₹{balanceRemaining.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
            </div>
          </div>
        </div>

        {/* Footer Actions */}
        <div className="p-4 border-t border-sasan-border bg-sasan-card flex items-center justify-end space-x-3">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 rounded-lg text-xs font-bold text-sasan-muted hover:text-sasan-text hover:bg-slate-800 transition-colors"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={handleCheckout}
            disabled={!isSettled || isProcessing}
            className={`inline-flex items-center space-x-2 px-6 py-2.5 rounded-xl text-xs font-black transition-all shadow-lg ${
              isSettled && !isProcessing
                ? 'bg-sasan-cyan hover:bg-cyan-400 text-slate-950 shadow-cyan-950/60 cursor-pointer'
                : 'bg-slate-800 text-slate-500 border border-slate-700 cursor-not-allowed'
            }`}
          >
            <Printer className="w-4 h-4" />
            <span>{isProcessing ? 'Saving & Printing...' : 'Save & Print Invoice [F9]'}</span>
          </button>
        </div>
      </div>
    </div>
  );
};
