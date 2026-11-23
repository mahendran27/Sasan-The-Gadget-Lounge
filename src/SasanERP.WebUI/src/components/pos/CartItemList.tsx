import React from 'react';
import { Trash2, Plus, Minus, Smartphone, Tag, ShoppingCart } from 'lucide-react';
import { Product, SerializedUnit } from '../../types/domain';

export interface CartItem {
  id: string;
  product: Product;
  unit?: SerializedUnit;
  quantity: number;
  unitPrice: number;
  discount: number;
  isRule32_5Margin: boolean;
}

interface CartItemListProps {
  items: CartItem[];
  onUpdateQuantity: (id: string, delta: number) => void;
  onUpdatePrice: (id: string, newPrice: number) => void;
  onRemoveItem: (id: string) => void;
  onClearCart: () => void;
}

export const CartItemList: React.FC<CartItemListProps> = ({
  items,
  onUpdateQuantity,
  onRemoveItem,
  onClearCart
}) => {
  if (items.length === 0) {
    return (
      <div className="bg-sasan-surface border border-sasan-border rounded-xl p-12 flex flex-col items-center justify-center text-center">
        <div className="w-16 h-16 rounded-full bg-slate-800/60 border border-slate-700/60 flex items-center justify-center mb-4 text-sasan-muted">
          <ShoppingCart className="w-8 h-8" />
        </div>
        <h3 className="text-sm font-bold text-sasan-text">POS Billing Cart is Empty</h3>
        <p className="text-xs text-sasan-muted mt-1 max-w-sm">
          Scan any device IMEI barcode or choose from product search above to begin billing.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between px-1">
        <span className="text-xs font-bold text-sasan-muted uppercase tracking-wider">
          Cart Items ({items.length})
        </span>
        <button
          onClick={onClearCart}
          className="text-[11px] text-rose-400 hover:text-rose-300 font-medium hover:underline"
        >
          Clear Cart
        </button>
      </div>

      <div className="overflow-x-auto rounded-xl border border-sasan-border bg-sasan-surface">
        <table className="w-full text-left text-xs">
          <thead className="bg-sasan-card text-sasan-muted uppercase text-[10px] tracking-wider border-b border-sasan-border">
            <tr>
              <th className="py-3 px-3">Item Description</th>
              <th className="py-3 px-3">Tax Scheme</th>
              <th className="py-3 px-3 text-center">Qty</th>
              <th className="py-3 px-3 text-right">Rate (₹)</th>
              <th className="py-3 px-3 text-right">Amount (₹)</th>
              <th className="py-3 px-3 text-center w-12">Action</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-sasan-border">
            {items.map((item) => {
              const lineTotal = item.quantity * item.unitPrice - item.discount;
              return (
                <tr key={item.id} className="hover:bg-slate-800/30 transition-colors">
                  <td className="py-3 px-3">
                    <div className="font-bold text-sasan-text text-xs flex items-center space-x-1.5">
                      {item.unit ? (
                        <Smartphone className="w-3.5 h-3.5 text-sasan-cyan flex-shrink-0" />
                      ) : (
                        <Tag className="w-3.5 h-3.5 text-sky-400 flex-shrink-0" />
                      )}
                      <span>{item.product.name}</span>
                    </div>
                    {item.unit && (
                      <div className="font-mono text-[11px] text-cyan-300 mt-0.5 flex items-center space-x-2">
                        <span>IMEI: {item.unit.imei1}</span>
                        <span>•</span>
                        <span>{item.unit.color}</span>
                        <span>•</span>
                        <span className="text-amber-400 font-bold">{item.unit.condition}</span>
                      </div>
                    )}
                    {!item.unit && (
                      <div className="font-mono text-[11px] text-sasan-muted mt-0.5">
                        SKU: {item.product.sku} | HSN: {item.product.hsnCode}
                      </div>
                    )}
                  </td>

                  <td className="py-3 px-3">
                    {item.isRule32_5Margin ? (
                      <span className="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider bg-emerald-950/80 border border-emerald-600/50 text-emerald-300">
                        Rule 32(5) Margin
                      </span>
                    ) : (
                      <span className="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider bg-slate-800 border border-slate-700 text-slate-300">
                        Section 9 (18% GST)
                      </span>
                    )}
                  </td>

                  <td className="py-3 px-3 text-center">
                    {item.unit ? (
                      <span className="font-mono font-bold text-xs px-2 py-1 bg-sasan-card rounded border border-sasan-border">
                        1
                      </span>
                    ) : (
                      <div className="inline-flex items-center border border-sasan-border rounded-lg bg-sasan-card">
                        <button
                          onClick={() => onUpdateQuantity(item.id, -1)}
                          className="p-1 hover:bg-slate-700 text-sasan-muted hover:text-sasan-text rounded-l"
                        >
                          <Minus className="w-3 h-3" />
                        </button>
                        <span className="font-mono font-bold px-2.5 text-xs text-sasan-text">
                          {item.quantity}
                        </span>
                        <button
                          onClick={() => onUpdateQuantity(item.id, 1)}
                          className="p-1 hover:bg-slate-700 text-sasan-muted hover:text-sasan-text rounded-r"
                        >
                          <Plus className="w-3 h-3" />
                        </button>
                      </div>
                    )}
                  </td>

                  <td className="py-3 px-3 text-right font-mono font-semibold text-sasan-text">
                    ₹{item.unitPrice.toLocaleString('en-IN')}
                  </td>

                  <td className="py-3 px-3 text-right font-mono font-bold text-sm text-emerald-400">
                    ₹{lineTotal.toLocaleString('en-IN')}
                  </td>

                  <td className="py-3 px-3 text-center">
                    <button
                      onClick={() => onRemoveItem(item.id)}
                      className="p-1.5 text-sasan-muted hover:text-rose-400 hover:bg-rose-950/40 rounded transition-colors"
                      title="Remove Item"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
};
