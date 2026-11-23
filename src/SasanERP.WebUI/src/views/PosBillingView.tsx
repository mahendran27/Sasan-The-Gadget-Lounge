import React, { useState } from 'react';
import {
  User,
  Plus,
  CreditCard,
  Printer,
  Sparkles,
  ShoppingBag,
  Percent,
  Check
} from 'lucide-react';
import { BarcodeScannerInput } from '../components/pos/BarcodeScannerInput';
import { CartItemList, CartItem } from '../components/pos/CartItemList';
import { SplitPaymentModal } from '../components/pos/SplitPaymentModal';
import { InvoicePreviewDrawer } from '../components/pos/InvoicePreviewDrawer';
import { Product, SerializedUnit, Customer } from '../types/domain';

interface PosBillingViewProps {
  onNavigate?: (module: string) => void;
}

export const PosBillingView: React.FC<PosBillingViewProps> = () => {
  const [cartItems, setCartItems] = useState<CartItem[]>([
    {
      id: 'demo-item-1',
      product: {
        id: 102,
        name: 'Samsung Galaxy S24 (8GB / 256GB)',
        brand: 'Samsung',
        category: 'Smartphone',
        sku: 'IMEI-829104',
        hsnCode: '85171300',
        isSerialized: true,
        defaultCostPrice: 48000,
        mrp: 65000,
        sellingPrice: 56000,
        currentStock: 1,
        reorderLevel: 1
      },
      unit: {
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
      quantity: 1,
      unitPrice: 56000,
      discount: 0,
      isRule32_5Margin: true
    }
  ]);

  const [customer, setCustomer] = useState<Customer>({
    id: 1,
    fullName: 'Walk-in Retail Customer',
    phoneNumber: '9080803070',
    outstandingCredit: 0
  });

  const [isCustomerModalOpen, setIsCustomerModalOpen] = useState(false);
  const [newCustomerName, setNewCustomerName] = useState('');
  const [newCustomerPhone, setNewCustomerPhone] = useState('');

  const [discountAmount, setDiscountAmount] = useState(0);
  const [isSplitModalOpen, setIsSplitModalOpen] = useState(false);
  const [isPreviewOpen, setIsPreviewOpen] = useState(false);
  const [createdInvoiceId, setCreatedInvoiceId] = useState<number | undefined>(undefined);
  const [createdInvoiceNumber, setCreatedInvoiceNumber] = useState<string>('SGL/26-27/0001');
  const [isProcessing, setIsProcessing] = useState(false);

  // Cart calculations
  const subTotal = cartItems.reduce((acc, item) => acc + item.quantity * item.unitPrice, 0);
  const grandTotal = Math.max(0, subTotal - discountAmount);

  // Margin scheme tax summary
  const marginItems = cartItems.filter((i) => i.isRule32_5Margin);
  const marginProfit = marginItems.reduce(
    (acc, item) => acc + Math.max(0, item.unitPrice - (item.unit?.purchaseCost || 0)),
    0
  );
  const estimatedMarginGst = (marginProfit * 18) / 118;

  const handleSelectItem = ({ product, unit }: { product: Product; unit?: SerializedUnit }) => {
    // If unit already in cart, avoid duplicate
    if (unit && cartItems.some((ci) => ci.unit?.id === unit.id)) {
      alert(`Device with IMEI ${unit.imei1} is already in the cart!`);
      return;
    }

    const newItem: CartItem = {
      id: `${product.id}-${unit?.id || Date.now()}`,
      product,
      unit,
      quantity: 1,
      unitPrice: unit ? unit.sellingPrice : product.sellingPrice,
      discount: 0,
      isRule32_5Margin: !!unit
    };
    setCartItems((prev) => [...prev, newItem]);
  };

  const handleUpdateQuantity = (id: string, delta: number) => {
    setCartItems((prev) =>
      prev
        .map((item) => {
          if (item.id === id) {
            const nextQty = item.quantity + delta;
            return nextQty > 0 ? { ...item, quantity: nextQty } : null;
          }
          return item;
        })
        .filter(Boolean) as CartItem[]
    );
  };

  const handleRemoveItem = (id: string) => {
    setCartItems((prev) => prev.filter((item) => item.id !== id));
  };

  const handleClearCart = () => {
    setCartItems([]);
    setDiscountAmount(0);
  };

  const handleAddCustomer = async () => {
    if (!newCustomerName.trim() || !newCustomerPhone.trim()) return;
    try {
      const res = await fetch('http://localhost:5000/api/pos/customers', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          fullName: newCustomerName.trim(),
          phoneNumber: newCustomerPhone.trim(),
          outstandingCredit: 0
        })
      });
      if (res.ok) {
        const saved = await res.json();
        setCustomer(saved);
      } else {
        setCustomer({
          id: Date.now(),
          fullName: newCustomerName.trim(),
          phoneNumber: newCustomerPhone.trim(),
          outstandingCredit: 0
        });
      }
    } catch {
      setCustomer({
        id: Date.now(),
        fullName: newCustomerName.trim(),
        phoneNumber: newCustomerPhone.trim(),
        outstandingCredit: 0
      });
    }
    setIsCustomerModalOpen(false);
    setNewCustomerName('');
    setNewCustomerPhone('');
  };

  const handleConfirmCheckout = async (
    payments: Array<{ mode: string; amount: number; reference?: string }>
  ) => {
    setIsProcessing(true);
    try {
      const payload = {
        customerId: customer.id,
        items: cartItems.map((ci) => ({
          productId: ci.product.id,
          serializedUnitId: ci.unit?.id,
          quantity: ci.quantity,
          unitPrice: ci.unitPrice,
          discountAmount: ci.discount,
          isSecondHandMarginScheme: ci.isRule32_5Margin,
          hsnCode: ci.product.hsnCode
        })),
        payments
      };

      const res = await fetch('http://localhost:5000/api/pos/invoice/checkout', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });

      if (res.ok) {
        const data = await res.json();
        setCreatedInvoiceId(data.invoiceId);
        setCreatedInvoiceNumber(data.invoiceNumber || 'SGL/26-27/0001');
      } else {
        setCreatedInvoiceId(1);
        setCreatedInvoiceNumber('SGL/26-27/0001');
      }
    } catch {
      setCreatedInvoiceId(1);
      setCreatedInvoiceNumber('SGL/26-27/0001');
    } finally {
      setIsProcessing(false);
      setIsSplitModalOpen(false);
      setIsPreviewOpen(true);
    }
  };

  const handleStartNewSale = () => {
    setIsPreviewOpen(false);
    setCartItems([]);
    setDiscountAmount(0);
  };

  return (
    <div className="p-6 space-y-6 max-w-[1920px] mx-auto">
      {/* Top Search & Scanner Bar */}
      <div className="flex items-center justify-between gap-4">
        <BarcodeScannerInput onSelectItem={handleSelectItem} />
      </div>

      {/* Main POS Layout: Left Cart, Right Checkout */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Left Column: Cart Items (8 cols) */}
        <div className="lg:col-span-8 space-y-4">
          <CartItemList
            items={cartItems}
            onUpdateQuantity={handleUpdateQuantity}
            onUpdatePrice={() => {}}
            onRemoveItem={handleRemoveItem}
            onClearCart={handleClearCart}
          />
        </div>

        {/* Right Column: Customer & Tender Breakdown (4 cols) */}
        <div className="lg:col-span-4 space-y-4">
          {/* Customer Selection Card */}
          <div className="bg-sasan-surface border border-sasan-border rounded-xl p-4 space-y-3">
            <div className="flex items-center justify-between pb-2 border-b border-sasan-border">
              <span className="text-xs font-bold uppercase tracking-wider text-sasan-muted flex items-center space-x-1.5">
                <User className="w-3.5 h-3.5 text-sasan-cyan" />
                <span>Customer Details</span>
              </span>
              <button
                onClick={() => setIsCustomerModalOpen(true)}
                className="text-[11px] font-bold text-sasan-cyan hover:underline inline-flex items-center space-x-0.5"
              >
                <Plus className="w-3 h-3" />
                <span>Change / Add</span>
              </button>
            </div>

            <div className="bg-sasan-card p-3 rounded-lg border border-sasan-border flex items-center justify-between">
              <div>
                <div className="font-bold text-xs text-sasan-text">
                  {customer.fullName}
                </div>
                <div className="font-mono text-[11px] text-sasan-muted mt-0.5">
                  Ph: {customer.phoneNumber}
                </div>
              </div>
              <span className="px-2 py-0.5 rounded text-[10px] font-bold uppercase bg-slate-800 border border-slate-700 text-slate-300">
                Active Buyer
              </span>
            </div>
          </div>

          {/* Pricing & GST Breakdown Card */}
          <div className="bg-sasan-surface border border-sasan-border rounded-xl p-4 space-y-3">
            <div className="text-xs font-bold uppercase tracking-wider text-sasan-muted pb-2 border-b border-sasan-border">
              Financial Summary
            </div>

            <div className="space-y-2 text-xs">
              <div className="flex items-center justify-between text-sasan-muted">
                <span>Subtotal ({cartItems.length} items)</span>
                <span className="font-mono font-semibold text-sasan-text">
                  ₹{subTotal.toLocaleString('en-IN')}
                </span>
              </div>

              {marginProfit > 0 && (
                <div className="flex items-center justify-between text-emerald-400 bg-emerald-950/30 p-2 rounded-lg border border-emerald-800/40">
                  <div className="flex items-center space-x-1">
                    <Sparkles className="w-3.5 h-3.5" />
                    <span>Rule 32(5) Margin Tax (18%)</span>
                  </div>
                  <span className="font-mono font-bold">
                    ₹{estimatedMarginGst.toLocaleString('en-IN', { maximumFractionDigits: 0 })}
                  </span>
                </div>
              )}

              <div className="flex items-center justify-between text-sasan-muted pt-1">
                <span className="flex items-center space-x-1">
                  <Percent className="w-3.5 h-3.5" />
                  <span>Discount</span>
                </span>
                <input
                  type="number"
                  value={discountAmount || ''}
                  onChange={(e) => setDiscountAmount(parseFloat(e.target.value) || 0)}
                  placeholder="0"
                  className="w-24 bg-sasan-card border border-sasan-border rounded px-2 py-1 text-right font-mono text-xs text-sasan-text focus:outline-none focus:border-sasan-cyan"
                />
              </div>

              <div className="border-t border-sasan-border pt-3 flex items-baseline justify-between">
                <div>
                  <div className="text-[10px] uppercase font-bold text-sasan-muted tracking-wider">
                    Grand Total
                  </div>
                  <div className="text-[10px] text-emerald-400 font-semibold">
                    Inclusive of all taxes
                  </div>
                </div>
                <div className="text-2xl font-black font-mono text-sasan-cyan tracking-tight">
                  ₹{grandTotal.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
                </div>
              </div>
            </div>

            {/* Main Action Buttons */}
            <div className="pt-2 space-y-2">
              <button
                onClick={() => setIsSplitModalOpen(true)}
                disabled={cartItems.length === 0}
                className={`w-full py-3 rounded-xl text-xs font-black uppercase tracking-wider flex items-center justify-center space-x-2 transition-all shadow-lg ${
                  cartItems.length > 0
                    ? 'bg-sasan-cyan hover:bg-cyan-400 text-slate-950 shadow-cyan-950/60 cursor-pointer'
                    : 'bg-slate-800 text-slate-500 border border-slate-700 cursor-not-allowed'
                }`}
              >
                <CreditCard className="w-4 h-4" />
                <span>Tender Split &amp; Pay [F9]</span>
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Add / Select Customer Modal */}
      {isCustomerModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 backdrop-blur-sm p-4">
          <div className="bg-sasan-surface border border-sasan-border rounded-xl w-full max-w-md p-5 space-y-4 shadow-2xl">
            <div className="flex items-center justify-between pb-2 border-b border-sasan-border">
              <h3 className="text-xs font-bold uppercase tracking-wider text-sasan-text">
                Quick Add / Select Customer
              </h3>
              <button
                onClick={() => setIsCustomerModalOpen(false)}
                className="text-xs text-sasan-muted hover:text-sasan-text"
              >
                ✕
              </button>
            </div>
            <div className="space-y-3">
              <div>
                <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                  Customer Name
                </label>
                <input
                  type="text"
                  value={newCustomerName}
                  onChange={(e) => setNewCustomerName(e.target.value)}
                  placeholder="e.g. Anand Kumar"
                  className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-sasan-text focus:outline-none focus:border-sasan-cyan"
                />
              </div>
              <div>
                <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                  Phone Number
                </label>
                <input
                  type="text"
                  value={newCustomerPhone}
                  onChange={(e) => setNewCustomerPhone(e.target.value)}
                  placeholder="e.g. 9842012345"
                  className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-sasan-text font-mono focus:outline-none focus:border-sasan-cyan"
                />
              </div>
            </div>
            <div className="flex items-center justify-end space-x-2 pt-2">
              <button
                onClick={() => setIsCustomerModalOpen(false)}
                className="px-3 py-1.5 rounded text-xs text-sasan-muted hover:text-sasan-text"
              >
                Cancel
              </button>
              <button
                onClick={handleAddCustomer}
                className="px-4 py-1.5 rounded bg-sasan-cyan hover:bg-cyan-400 text-slate-950 font-bold text-xs"
              >
                Save &amp; Select
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Split Payment Modal */}
      <SplitPaymentModal
        isOpen={isSplitModalOpen}
        onClose={() => setIsSplitModalOpen(false)}
        grandTotal={grandTotal}
        customer={customer}
        onConfirmCheckout={handleConfirmCheckout}
        isProcessing={isProcessing}
      />

      {/* QuestPDF Invoice Preview Drawer */}
      <InvoicePreviewDrawer
        isOpen={isPreviewOpen}
        onClose={() => setIsPreviewOpen(false)}
        invoiceId={createdInvoiceId}
        invoiceNumber={createdInvoiceNumber}
        onStartNewSale={handleStartNewSale}
      />
    </div>
  );
};
