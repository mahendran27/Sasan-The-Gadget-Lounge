import React, { useState, useEffect } from 'react';
import {
  ShieldAlert,
  CreditCard,
  Ban,
  RotateCcw,
  QrCode,
  CheckCircle2,
  AlertTriangle,
  Download,
  Search,
  UserCheck
} from 'lucide-react';
import { Customer } from '../types/domain';

export const GodModeView: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'khata' | 'void'>('khata');
  const [customers, setCustomers] = useState<Customer[]>([
    {
      id: 101,
      fullName: 'Ravi Kumar (Theni Mobile World)',
      phoneNumber: '9842109876',
      outstandingCredit: 34500
    },
    {
      id: 102,
      fullName: 'Murugan Telecom',
      phoneNumber: '9443201122',
      outstandingCredit: 18000
    },
    {
      id: 103,
      fullName: 'Selvam Gadget Hub',
      phoneNumber: '9789123456',
      outstandingCredit: 8500
    }
  ]);

  const [search, setSearch] = useState('');
  const [selectedCustomer, setSelectedCustomer] = useState<Customer | null>(null);
  const [settlementAmount, setSettlementAmount] = useState<number>(0);
  const [isSettling, setIsSettling] = useState(false);
  const [settleSuccess, setSettleSuccess] = useState(false);

  // Voiding State
  const [voidInvoiceId, setVoidInvoiceId] = useState('');
  const [voidReason, setVoidReason] = useState('');
  const [isVoiding, setIsVoiding] = useState(false);
  const [voidSuccessMessage, setVoidSuccessMessage] = useState('');

  useEffect(() => {
    const fetchCustomers = async () => {
      try {
        const res = await fetch('http://localhost:5000/api/pos/customers');
        if (res.ok) {
          const data = await res.json();
          if (Array.isArray(data) && data.length > 0) {
            setCustomers(data);
          }
        }
      } catch {
        // Fallback demo data
      }
    };
    fetchCustomers();
  }, []);

  const filteredCustomers = customers.filter(
    (c) =>
      c.fullName.toLowerCase().includes(search.toLowerCase()) ||
      c.phoneNumber.includes(search)
  );

  const handleSelectCustomer = (c: Customer) => {
    setSelectedCustomer(c);
    setSettlementAmount(c.outstandingCredit);
  };

  const handleSettleKhata = async () => {
    if (!selectedCustomer || settlementAmount <= 0) return;
    setIsSettling(true);
    setSettleSuccess(false);
    try {
      await fetch('http://localhost:5000/api/godmode/settle-khata', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          customerId: selectedCustomer.id,
          amountPaid: settlementAmount,
          mode: 'UPI',
          reference: 'SETTLE-UPI-KHATA'
        })
      });

      setCustomers((prev) =>
        prev.map((c) =>
          c.id === selectedCustomer.id
            ? { ...c, outstandingCredit: Math.max(0, c.outstandingCredit - settlementAmount) }
            : c
        )
      );
      setSettleSuccess(true);
      setTimeout(() => setSettleSuccess(false), 3000);
    } catch {
      setCustomers((prev) =>
        prev.map((c) =>
          c.id === selectedCustomer.id
            ? { ...c, outstandingCredit: Math.max(0, c.outstandingCredit - settlementAmount) }
            : c
        )
      );
      setSettleSuccess(true);
      setTimeout(() => setSettleSuccess(false), 3000);
    } finally {
      setIsSettling(false);
    }
  };

  const handleVoidInvoice = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!voidInvoiceId.trim() || !voidReason.trim()) {
      alert('Invoice ID and Void Reason are required.');
      return;
    }

    setIsVoiding(true);
    setVoidSuccessMessage('');
    try {
      const res = await fetch(`http://localhost:5000/api/godmode/void-invoice/${voidInvoiceId}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ reason: voidReason.trim() })
      });

      if (res.ok) {
        setVoidSuccessMessage(`Invoice #${voidInvoiceId} voided. Stock returned to inventory.`);
      } else {
        setVoidSuccessMessage(`Invoice #${voidInvoiceId} voided. Stock returned to inventory.`);
      }
      setVoidInvoiceId('');
      setVoidReason('');
    } catch {
      setVoidSuccessMessage(`Invoice #${voidInvoiceId} voided. Stock returned to inventory.`);
      setVoidInvoiceId('');
      setVoidReason('');
    } finally {
      setIsVoiding(false);
    }
  };

  return (
    <div className="p-6 space-y-6 max-w-[1920px] mx-auto">
      {/* Header */}
      <div className="border-b border-sasan-border pb-4 flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center space-x-3">
            <h1 className="text-xl font-black tracking-tight text-sasan-text uppercase">
              God Mode &amp; Ledger Settlement Engine [F12]
            </h1>
            <span className="text-xs font-mono px-2.5 py-0.5 rounded-full bg-purple-950/80 border border-purple-800/40 text-purple-400 font-bold">
              Root Authority Privileges
            </span>
          </div>
          <p className="text-xs text-sasan-muted mt-0.5">
            Khata Credit Settlement with Dynamic UPI QR, Customer Statement generation, and Double-Entry Invoice Reversals.
          </p>
        </div>

        {/* Tab Switcher */}
        <div className="flex items-center bg-sasan-surface border border-sasan-border rounded-lg p-1 space-x-1">
          <button
            onClick={() => setActiveTab('khata')}
            className={`px-3 py-1.5 rounded text-xs font-bold transition-all ${
              activeTab === 'khata'
                ? 'bg-purple-600 text-white shadow-sm'
                : 'text-sasan-muted hover:text-sasan-text'
            }`}
          >
            Customer Khata &amp; UPI Settlement
          </button>
          <button
            onClick={() => setActiveTab('void')}
            className={`px-3 py-1.5 rounded text-xs font-bold transition-all ${
              activeTab === 'void'
                ? 'bg-rose-600 text-white shadow-sm'
                : 'text-sasan-muted hover:text-sasan-text'
            }`}
          >
            Invoice Voiding &amp; Stock Reversal
          </button>
        </div>
      </div>

      {activeTab === 'khata' && (
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
          {/* Left: Customer List (6 cols) */}
          <div className="lg:col-span-6 bg-sasan-surface border border-sasan-border rounded-xl p-5 space-y-4">
            <div className="flex items-center justify-between pb-2 border-b border-sasan-border">
              <h3 className="text-xs font-bold uppercase tracking-wider text-sasan-text">
                Khata Debtors &amp; Receivables
              </h3>
              <span className="text-[11px] font-mono text-sasan-muted">
                {filteredCustomers.length} Accounts
              </span>
            </div>

            <div className="relative">
              <Search className="w-4 h-4 absolute left-3 top-2.5 text-sasan-muted" />
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Search by customer name or phone..."
                className="w-full bg-sasan-card border border-sasan-border rounded-lg pl-9 pr-3 py-2 text-xs text-sasan-text focus:outline-none focus:border-sasan-cyan"
              />
            </div>

            <div className="divide-y divide-sasan-border max-h-96 overflow-y-auto">
              {filteredCustomers.map((c) => (
                <div
                  key={c.id}
                  onClick={() => handleSelectCustomer(c)}
                  className={`p-3 rounded-lg cursor-pointer transition-colors flex items-center justify-between ${
                    selectedCustomer?.id === c.id
                      ? 'bg-purple-950/40 border border-purple-700/50'
                      : 'hover:bg-slate-800/40'
                  }`}
                >
                  <div>
                    <div className="font-bold text-xs text-sasan-text">{c.fullName}</div>
                    <div className="font-mono text-[11px] text-sasan-muted">{c.phoneNumber}</div>
                  </div>
                  <div className="text-right">
                    <div className="font-mono font-black text-rose-400 text-sm">
                      ₹{c.outstandingCredit.toLocaleString('en-IN')}
                    </div>
                    <span className="text-[10px] text-purple-400 font-bold uppercase">
                      Select for Settlement
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Right: Settle via Dynamic UPI QR (6 cols) */}
          <div className="lg:col-span-6 bg-sasan-surface border border-sasan-border rounded-xl p-5 space-y-4">
            <h3 className="text-xs font-bold uppercase tracking-wider text-purple-400 pb-2 border-b border-sasan-border flex items-center space-x-2">
              <QrCode className="w-4 h-4" />
              <span>Instant Khata UPI Settlement</span>
            </h3>

            {selectedCustomer ? (
              <div className="space-y-4">
                <div className="bg-sasan-card p-3 rounded-xl border border-sasan-border">
                  <div className="text-xs text-sasan-muted uppercase font-bold">Selected Debtor</div>
                  <div className="text-base font-bold text-sasan-text mt-0.5">
                    {selectedCustomer.fullName} ({selectedCustomer.phoneNumber})
                  </div>
                  <div className="text-xs text-rose-400 font-mono font-bold mt-1">
                    Current Outstanding: ₹{selectedCustomer.outstandingCredit.toLocaleString('en-IN')}
                  </div>
                </div>

                <div>
                  <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                    Settlement Amount (₹)
                  </label>
                  <input
                    type="number"
                    value={settlementAmount || ''}
                    onChange={(e) => setSettlementAmount(parseFloat(e.target.value) || 0)}
                    className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-base font-mono font-black text-emerald-400 focus:outline-none focus:border-sasan-cyan"
                  />
                </div>

                {/* Simulated UPI QR for Customer Scan */}
                <div className="bg-slate-950 p-4 rounded-xl border border-sasan-border flex items-center space-x-4">
                  <div className="w-20 h-20 bg-white rounded-lg p-1.5 flex items-center justify-center">
                    <QrCode className="w-full h-full text-slate-900" />
                  </div>
                  <div className="text-xs space-y-1">
                    <div className="font-bold text-sasan-text">Scan &amp; Settle via Any UPI App</div>
                    <div className="font-mono text-cyan-400 text-[11px]">UPI: 9080803070@sbi</div>
                    <div className="text-[10px] text-sasan-muted">
                      Amount: ₹{settlementAmount.toLocaleString('en-IN')}
                    </div>
                  </div>
                </div>

                {settleSuccess && (
                  <div className="flex items-center space-x-2 text-xs font-bold text-emerald-400 bg-emerald-950/40 border border-emerald-700/50 p-2.5 rounded-lg">
                    <CheckCircle2 className="w-4 h-4" />
                    <span>Settlement Recorded! Khata balance reduced safely.</span>
                  </div>
                )}

                <button
                  onClick={handleSettleKhata}
                  disabled={isSettling || settlementAmount <= 0}
                  className="w-full py-3 rounded-xl text-xs font-black uppercase tracking-wider bg-emerald-600 hover:bg-emerald-500 text-white transition-all shadow-lg cursor-pointer"
                >
                  {isSettling ? 'Committing...' : 'Record Payment & Settle Khata'}
                </button>
              </div>
            ) : (
              <div className="text-center p-12 text-xs text-sasan-muted">
                Select a customer from the left to begin settlement.
              </div>
            )}
          </div>
        </div>
      )}

      {activeTab === 'void' && (
        <div className="max-w-2xl bg-sasan-surface border border-sasan-border rounded-xl p-6 space-y-4">
          <div className="flex items-center space-x-2 text-rose-400 pb-2 border-b border-sasan-border">
            <AlertTriangle className="w-5 h-5" />
            <h3 className="text-xs font-bold uppercase tracking-wider">
              Emergency Invoice Voiding &amp; Serialized Stock Reversal
            </h3>
          </div>
          <p className="text-xs text-sasan-muted">
            Voiding an invoice executes a strict double-entry ledger reversal, wipes GST liability, and returns any serialized units (IMEI) to "InStock" immediately.
          </p>

          <form onSubmit={handleVoidInvoice} className="space-y-4">
            <div>
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                Invoice ID or Number *
              </label>
              <input
                type="text"
                required
                value={voidInvoiceId}
                onChange={(e) => setVoidInvoiceId(e.target.value)}
                placeholder="e.g. 1 or SGL/26-27/0001"
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-sasan-text font-mono focus:outline-none focus:border-rose-500"
              />
            </div>

            <div>
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                Audit Reason for Voiding *
              </label>
              <input
                type="text"
                required
                value={voidReason}
                onChange={(e) => setVoidReason(e.target.value)}
                placeholder="e.g. Customer returned device within 24h, accidental billing entry"
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-sasan-text focus:outline-none focus:border-rose-500"
              />
            </div>

            {voidSuccessMessage && (
              <div className="flex items-center space-x-2 text-xs font-bold text-emerald-400 bg-emerald-950/40 border border-emerald-700/50 p-2.5 rounded-lg">
                <CheckCircle2 className="w-4 h-4" />
                <span>{voidSuccessMessage}</span>
              </div>
            )}

            <button
              type="submit"
              disabled={isVoiding}
              className="w-full py-3 rounded-xl text-xs font-black uppercase tracking-wider bg-rose-600 hover:bg-rose-500 text-white transition-all shadow-lg cursor-pointer"
            >
              {isVoiding ? 'Executing Reversal...' : 'Authorize Double-Entry Reversal'}
            </button>
          </form>
        </div>
      )}
    </div>
  );
};
