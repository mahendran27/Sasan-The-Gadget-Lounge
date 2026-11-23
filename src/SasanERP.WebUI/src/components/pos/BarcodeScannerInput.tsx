import React, { useState, useEffect, useRef } from 'react';
import { Search, Barcode, Smartphone, Plus } from 'lucide-react';
import { Product, SerializedUnit } from '../../types/domain';

interface BarcodeScannerInputProps {
  onSelectItem: (item: { product: Product; unit?: SerializedUnit }) => void;
}

export const BarcodeScannerInput: React.FC<BarcodeScannerInputProps> = ({ onSelectItem }) => {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<{ products: Product[]; units: SerializedUnit[] }>({
    products: [],
    units: []
  });
  const [isOpen, setIsOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  // Global listener for scanner or F2
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'F2') {
        e.preventDefault();
        inputRef.current?.focus();
        inputRef.current?.select();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

  useEffect(() => {
    if (!query.trim()) {
      setResults({ products: [], units: [] });
      setIsOpen(false);
      return;
    }

    const timer = setTimeout(async () => {
      setLoading(true);
      try {
        const res = await fetch(`http://localhost:5000/api/products/search?q=${encodeURIComponent(query)}`);
        if (res.ok) {
          const data = await res.json();
          setResults({
            products: data.products || [],
            units: data.serializedUnits || []
          });
          setIsOpen(true);
        }
      } catch {
        // Fallback demo mock in offline mode
        setResults({
          products: [
            {
              id: 101,
              name: 'Type-C 65W Fast Charger',
              brand: 'Samsung',
              category: 'Accessory',
              sku: 'ACC-CHG-65W',
              hsnCode: '85044030',
              isSerialized: false,
              defaultCostPrice: 850,
              mrp: 1999,
              sellingPrice: 1499,
              currentStock: 12,
              reorderLevel: 5
            }
          ],
          units: [
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
            }
          ]
        });
        setIsOpen(true);
      } finally {
        setLoading(false);
      }
    }, 200);

    return () => clearTimeout(timer);
  }, [query]);

  const handleSelectProduct = (product: Product) => {
    onSelectItem({ product });
    setQuery('');
    setIsOpen(false);
    inputRef.current?.focus();
  };

  const handleSelectUnit = (unit: SerializedUnit) => {
    const mockProduct: Product = {
      id: unit.productId,
      name: unit.productName,
      brand: 'Samsung',
      category: 'Smartphone',
      sku: `IMEI-${unit.imei1.slice(-6)}`,
      hsnCode: '85171300',
      isSerialized: true,
      defaultCostPrice: unit.purchaseCost,
      mrp: unit.sellingPrice + 5000,
      sellingPrice: unit.sellingPrice,
      currentStock: 1,
      reorderLevel: 1
    };
    onSelectItem({ product: mockProduct, unit });
    setQuery('');
    setIsOpen(false);
    inputRef.current?.focus();
  };

  return (
    <div className="relative w-full">
      <div className="relative flex items-center">
        <div className="absolute left-3 text-sasan-cyan flex items-center pointer-events-none">
          <Barcode className="w-5 h-5 mr-1 text-sasan-cyan" />
        </div>
        <input
          ref={inputRef}
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Scan IMEI / Barcode, or search device model, SKU, accessory... [F2 to focus]"
          className="w-full bg-sasan-card border border-sasan-border focus:border-sasan-cyan rounded-xl py-3 pl-12 pr-10 text-sm text-sasan-text placeholder:text-sasan-muted focus:outline-none focus:ring-1 focus:ring-sasan-cyan transition-all font-mono"
        />
        {query && (
          <button
            onClick={() => {
              setQuery('');
              setIsOpen(false);
            }}
            className="absolute right-3 text-xs text-sasan-muted hover:text-sasan-text px-1.5 py-0.5 rounded bg-slate-800"
          >
            ESC
          </button>
        )}
      </div>

      {/* Autocomplete Dropdown */}
      {isOpen && (
        <div className="absolute z-50 left-0 right-0 mt-2 bg-sasan-surface border border-sasan-border rounded-xl shadow-2xl max-h-96 overflow-y-auto divide-y divide-sasan-border">
          {/* Serialized Devices */}
          {results.units.length > 0 && (
            <div className="p-2">
              <div className="px-2 py-1 text-[10px] font-bold uppercase tracking-wider text-sasan-cyan flex items-center space-x-1">
                <Smartphone className="w-3 h-3" />
                <span>Pre-Owned Serialized Gadgets (Rule 32(5) Scheme)</span>
              </div>
              {results.units.map((unit) => (
                <button
                  key={unit.id}
                  onClick={() => handleSelectUnit(unit)}
                  className="w-full text-left p-2.5 rounded-lg hover:bg-slate-800/60 transition-colors flex items-center justify-between group"
                >
                  <div>
                    <div className="font-semibold text-xs text-sasan-text group-hover:text-sasan-cyan transition-colors">
                      {unit.productName}
                    </div>
                    <div className="flex items-center space-x-2 text-[11px] text-sasan-muted mt-0.5 font-mono">
                      <span>IMEI: {unit.imei1}</span>
                      <span>•</span>
                      <span>{unit.storageCapacity}</span>
                      <span>•</span>
                      <span className="text-amber-400 font-bold">{unit.condition}</span>
                    </div>
                  </div>
                  <div className="text-right">
                    <div className="font-mono font-bold text-sm text-emerald-400">
                      ₹{unit.sellingPrice.toLocaleString('en-IN')}
                    </div>
                    <div className="text-[10px] text-cyan-400 font-bold uppercase">
                      + Add Device
                    </div>
                  </div>
                </button>
              ))}
            </div>
          )}

          {/* Standard Retail Accessories */}
          {results.products.length > 0 && (
            <div className="p-2">
              <div className="px-2 py-1 text-[10px] font-bold uppercase tracking-wider text-sasan-muted flex items-center space-x-1">
                <Barcode className="w-3 h-3" />
                <span>Retail Stock & Accessories</span>
              </div>
              {results.products.map((product) => (
                <button
                  key={product.id}
                  onClick={() => handleSelectProduct(product)}
                  className="w-full text-left p-2.5 rounded-lg hover:bg-slate-800/60 transition-colors flex items-center justify-between group"
                >
                  <div>
                    <div className="font-semibold text-xs text-sasan-text group-hover:text-sasan-cyan transition-colors">
                      {product.name}
                    </div>
                    <div className="flex items-center space-x-2 text-[11px] text-sasan-muted mt-0.5 font-mono">
                      <span>SKU: {product.sku}</span>
                      <span>•</span>
                      <span>Stock: {product.currentStock}</span>
                    </div>
                  </div>
                  <div className="text-right">
                    <div className="font-mono font-bold text-sm text-emerald-400">
                      ₹{product.sellingPrice.toLocaleString('en-IN')}
                    </div>
                    <div className="text-[10px] text-sky-400 font-bold uppercase">
                      + Add Item
                    </div>
                  </div>
                </button>
              ))}
            </div>
          )}

          {results.units.length === 0 && results.products.length === 0 && !loading && (
            <div className="p-6 text-center text-xs text-sasan-muted">
              No matching devices or accessories found for "{query}".
            </div>
          )}
        </div>
      )}
    </div>
  );
};
