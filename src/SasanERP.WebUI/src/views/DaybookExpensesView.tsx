import React, { useState, useEffect } from 'react';
import {
  Wallet,
  TrendingDown,
  TrendingUp,
  Plus,
  Calendar,
  Banknote,
  DollarSign,
  Coffee,
  Truck,
  Wrench,
  UserCheck
} from 'lucide-react';

interface ExpenseEntry {
  id: number;
  date: string;
  category: string;
  description: string;
  amount: number;
  paymentMode: string;
  voucherNumber: string;
}

export const DaybookExpensesView: React.FC = () => {
  const [openingBalance, setOpeningBalance] = useState<number>(5000);
  const [todaySalesCash, setTodaySalesCash] = useState<number>(24500);
  const [entries, setEntries] = useState<ExpenseEntry[]>([
    {
      id: 1,
      date: new Date().toISOString(),
      category: 'Refreshments & Tea',
      description: 'Customer tea & water bottles',
      amount: 180,
      paymentMode: 'Cash',
      voucherNumber: 'EXP-001'
    },
    {
      id: 2,
      date: new Date().toISOString(),
      category: 'Courier & Logistics',
      description: 'Display folder courier from Chennai',
      amount: 450,
      paymentMode: 'Cash',
      voucherNumber: 'EXP-002'
    }
  ]);

  const [category, setCategory] = useState('Refreshments & Tea');
  const [description, setDescription] = useState('');
  const [amount, setAmount] = useState<number>(0);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const totalExpenses = entries.reduce((acc, e) => acc + e.amount, 0);
  const currentDrawerCash = openingBalance + todaySalesCash - totalExpenses;

  const handleAddExpense = async (e: React.FormEvent) => {
    e.preventDefault();
    if (amount <= 0 || !description.trim()) {
      alert('Please enter a valid amount and description.');
      return;
    }

    setIsSubmitting(true);
    try {
      const payload = {
        category,
        description: description.trim(),
        amount,
        paymentMode: 'Cash'
      };

      await fetch('http://localhost:5000/api/expenses', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });

      const newEntry: ExpenseEntry = {
        id: Date.now(),
        date: new Date().toISOString(),
        category,
        description: description.trim(),
        amount,
        paymentMode: 'Cash',
        voucherNumber: `EXP-${String(entries.length + 1).padStart(3, '0')}`
      };

      setEntries([newEntry, ...entries]);
      setDescription('');
      setAmount(0);
    } catch {
      const newEntry: ExpenseEntry = {
        id: Date.now(),
        date: new Date().toISOString(),
        category,
        description: description.trim(),
        amount,
        paymentMode: 'Cash',
        voucherNumber: `EXP-${String(entries.length + 1).padStart(3, '0')}`
      };
      setEntries([newEntry, ...entries]);
      setDescription('');
      setAmount(0);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="p-6 space-y-6 max-w-[1920px] mx-auto">
      {/* Header */}
      <div className="border-b border-sasan-border pb-4 flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center space-x-3">
            <h1 className="text-xl font-black tracking-tight text-sasan-text uppercase">
              Daybook, Cash Drawer &amp; Store Expenses [F6]
            </h1>
            <span className="text-xs font-mono px-2.5 py-0.5 rounded-full bg-emerald-950/80 border border-emerald-800/40 text-emerald-400 font-bold">
              Till Balance: ₹{currentDrawerCash.toLocaleString('en-IN')}
            </span>
          </div>
          <p className="text-xs text-sasan-muted mt-0.5">
            Real-time cash in drawer reconciliation, operating vouchers, and daily net EBITDA.
          </p>
        </div>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-sasan-surface border border-sasan-border rounded-xl p-4 flex flex-col justify-between">
          <div className="flex items-center justify-between">
            <span className="text-[11px] font-bold uppercase tracking-wider text-sasan-muted">
              Opening Float
            </span>
            <Wallet className="w-4 h-4 text-sasan-muted" />
          </div>
          <div className="mt-2 text-2xl font-black font-mono text-sasan-text">
            ₹{openingBalance.toLocaleString('en-IN')}
          </div>
        </div>

        <div className="bg-sasan-surface border border-sasan-border rounded-xl p-4 flex flex-col justify-between">
          <div className="flex items-center justify-between">
            <span className="text-[11px] font-bold uppercase tracking-wider text-sasan-muted">
              Today's Cash Inflow
            </span>
            <TrendingUp className="w-4 h-4 text-emerald-400" />
          </div>
          <div className="mt-2 text-2xl font-black font-mono text-emerald-400">
            ₹{todaySalesCash.toLocaleString('en-IN')}
          </div>
        </div>

        <div className="bg-sasan-surface border border-sasan-border rounded-xl p-4 flex flex-col justify-between">
          <div className="flex items-center justify-between">
            <span className="text-[11px] font-bold uppercase tracking-wider text-sasan-muted">
              Operating Expenses
            </span>
            <TrendingDown className="w-4 h-4 text-rose-400" />
          </div>
          <div className="mt-2 text-2xl font-black font-mono text-rose-400">
            ₹{totalExpenses.toLocaleString('en-IN')}
          </div>
        </div>

        <div className="bg-sasan-surface border border-sasan-border rounded-xl p-4 flex flex-col justify-between">
          <div className="flex items-center justify-between">
            <span className="text-[11px] font-bold uppercase tracking-wider text-sasan-muted">
              Closing Drawer Cash
            </span>
            <Banknote className="w-4 h-4 text-sasan-cyan" />
          </div>
          <div className="mt-2 text-2xl font-black font-mono text-sasan-cyan">
            ₹{currentDrawerCash.toLocaleString('en-IN')}
          </div>
        </div>
      </div>

      {/* Main Content: Add Expense Form & Voucher Table */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Form (4 cols) */}
        <div className="lg:col-span-4 bg-sasan-surface border border-sasan-border rounded-xl p-5 space-y-4">
          <h3 className="text-xs font-bold uppercase tracking-wider text-sasan-text flex items-center space-x-2 pb-2 border-b border-sasan-border">
            <Plus className="w-4 h-4 text-sasan-cyan" />
            <span>Record Store Expense Voucher</span>
          </h3>

          <form onSubmit={handleAddExpense} className="space-y-4">
            <div>
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                Expense Category
              </label>
              <select
                value={category}
                onChange={(e) => setCategory(e.target.value)}
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-sasan-text font-semibold focus:outline-none focus:border-sasan-cyan"
              >
                <option value="Refreshments & Tea">Refreshments &amp; Tea</option>
                <option value="Courier & Logistics">Courier &amp; Logistics</option>
                <option value="Store Maintenance">Store Maintenance</option>
                <option value="Packing & Printing">Packing &amp; Printing</option>
                <option value="Staff Advance">Staff Salary Advance</option>
                <option value="Electricity & Internet">Electricity &amp; Internet</option>
                <option value="Other">Other Operational</option>
              </select>
            </div>

            <div>
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                Description / Remarks *
              </label>
              <input
                type="text"
                required
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="e.g. Courier charges for tempered glass"
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-sasan-text focus:outline-none focus:border-sasan-cyan"
              />
            </div>

            <div>
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                Amount (₹) *
              </label>
              <input
                type="number"
                min={1}
                required
                value={amount || ''}
                onChange={(e) => setAmount(parseFloat(e.target.value) || 0)}
                placeholder="0"
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs font-mono font-bold text-rose-400 focus:outline-none focus:border-sasan-cyan"
              />
            </div>

            <button
              type="submit"
              disabled={isSubmitting}
              className="w-full py-2.5 rounded-xl text-xs font-black uppercase tracking-wider bg-rose-600 hover:bg-rose-500 text-white transition-all shadow-md cursor-pointer flex items-center justify-center space-x-1.5"
            >
              <Plus className="w-4 h-4" />
              <span>Record Expense Voucher</span>
            </button>
          </form>
        </div>

        {/* Voucher Table (8 cols) */}
        <div className="lg:col-span-8 bg-sasan-surface border border-sasan-border rounded-xl p-5 space-y-3">
          <div className="flex items-center justify-between pb-2 border-b border-sasan-border">
            <h3 className="text-xs font-bold uppercase tracking-wider text-sasan-text">
              Today's Operating Expense Vouchers
            </h3>
            <span className="text-[11px] font-mono text-sasan-muted">
              {entries.length} Vouchers Logged
            </span>
          </div>

          <div className="overflow-x-auto rounded-lg border border-sasan-border">
            <table className="w-full text-left text-xs">
              <thead className="bg-sasan-card text-sasan-muted uppercase text-[10px] tracking-wider border-b border-sasan-border">
                <tr>
                  <th className="py-2.5 px-3">Voucher #</th>
                  <th className="py-2.5 px-3">Category</th>
                  <th className="py-2.5 px-3">Description</th>
                  <th className="py-2.5 px-3">Time</th>
                  <th className="py-2.5 px-3 text-right">Amount (₹)</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-sasan-border bg-sasan-card">
                {entries.map((e) => (
                  <tr key={e.id} className="hover:bg-slate-800/30 transition-colors">
                    <td className="py-2.5 px-3 font-mono font-bold text-cyan-400">
                      {e.voucherNumber}
                    </td>
                    <td className="py-2.5 px-3 font-semibold text-sasan-text">
                      {e.category}
                    </td>
                    <td className="py-2.5 px-3 text-sasan-muted">
                      {e.description}
                    </td>
                    <td className="py-2.5 px-3 font-mono text-[11px] text-sasan-muted">
                      {new Date(e.date).toLocaleTimeString('en-IN', {
                        hour: '2-digit',
                        minute: '2-digit'
                      })}
                    </td>
                    <td className="py-2.5 px-3 text-right font-mono font-bold text-rose-400">
                      ₹{e.amount.toLocaleString('en-IN')}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
};
