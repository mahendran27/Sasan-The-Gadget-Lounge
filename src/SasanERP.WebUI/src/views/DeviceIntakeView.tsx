import React, { useState } from 'react';
import {
  Smartphone,
  CheckCircle2,
  Printer,
  Sparkles,
  ArrowRight,
  ShieldCheck,
  AlertCircle
} from 'lucide-react';
import { BarcodeStickerPrintModal } from '../components/barcode/BarcodeStickerPrintModal';
import { ConditionGrade, DeviceType, SerializedUnit } from '../types/domain';

export const DeviceIntakeView: React.FC = () => {
  const [productName, setProductName] = useState('');
  const [brand, setBrand] = useState('Apple');
  const [category, setCategory] = useState<DeviceType>('Smartphone');
  const [imei1, setImei1] = useState('');
  const [imei2, setImei2] = useState('');
  const [serialNumber, setSerialNumber] = useState('');
  const [color, setColor] = useState('Titanium Gray');
  const [storageCapacity, setStorageCapacity] = useState('256GB');
  const [conditionGrade, setConditionGrade] = useState<ConditionGrade>('GradeA_Excellent');
  const [batteryHealth, setBatteryHealth] = useState(96);
  const [purchaseCost, setPurchaseCost] = useState<number>(45000);
  const [taggedPrice, setTaggedPrice] = useState<number>(54000);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [intakeSuccessUnit, setIntakeSuccessUnit] = useState<SerializedUnit | undefined>(undefined);
  const [isPrintModalOpen, setIsPrintModalOpen] = useState(false);

  // Projected margin calculation under Rule 32(5)
  const projectedProfit = Math.max(0, taggedPrice - purchaseCost);
  const estimatedMarginGst = (projectedProfit * 18) / 118;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!productName.trim() || !imei1.trim()) {
      alert('Product Name and Primary IMEI 1 are mandatory.');
      return;
    }

    setIsSubmitting(true);
    try {
      const payload = {
        productId: 0,
        productName: productName.trim(),
        brand,
        category,
        imei1: imei1.trim(),
        imei2: imei2.trim() || undefined,
        serialNumber: serialNumber.trim() || undefined,
        color,
        storageCapacity,
        conditionGrade,
        batteryHealthPercentage: batteryHealth,
        purchaseCost,
        taggedPrice
      };

      const res = await fetch('http://localhost:5000/api/inventory/intake-unit', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });

      const unit: SerializedUnit = {
        id: Date.now(),
        productId: 100,
        productName: productName.trim(),
        imei1: imei1.trim(),
        serialNumber: serialNumber.trim(),
        color,
        storageCapacity,
        condition: conditionGrade,
        batteryHealthPercentage: batteryHealth,
        purchaseCost,
        sellingPrice: taggedPrice,
        agingDays: 0,
        status: 'InStock'
      };

      if (res.ok) {
        const data = await res.json();
        unit.id = data.unitId || unit.id;
      }

      setIntakeSuccessUnit(unit);
      setIsPrintModalOpen(true);

      // Reset fields
      setProductName('');
      setImei1('');
      setImei2('');
      setSerialNumber('');
    } catch {
      // Fallback
      const unit: SerializedUnit = {
        id: Date.now(),
        productId: 100,
        productName: productName.trim(),
        imei1: imei1.trim(),
        serialNumber: serialNumber.trim(),
        color,
        storageCapacity,
        condition: conditionGrade,
        batteryHealthPercentage: batteryHealth,
        purchaseCost,
        sellingPrice: taggedPrice,
        agingDays: 0,
        status: 'InStock'
      };
      setIntakeSuccessUnit(unit);
      setIsPrintModalOpen(true);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="p-6 space-y-6 max-w-5xl mx-auto">
      {/* Header */}
      <div className="border-b border-sasan-border pb-4">
        <div className="flex items-center space-x-3">
          <h1 className="text-xl font-black tracking-tight text-sasan-text uppercase">
            Device Intake &amp; Barcode Label Spooler [F5]
          </h1>
          <span className="text-xs font-bold uppercase tracking-wider px-2 py-0.5 rounded-full bg-amber-950/80 border border-amber-500/40 text-amber-400">
            Rule 32(5) Valuation
          </span>
        </div>
        <p className="text-xs text-sasan-muted mt-0.5">
          Take pre-owned gadgets or fresh inventory into stock and automatically generate a TSPL sticker.
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Section 1: Device Identification */}
        <div className="bg-sasan-surface border border-sasan-border rounded-xl p-5 space-y-4">
          <h3 className="text-xs font-bold uppercase tracking-wider text-sasan-cyan flex items-center space-x-2">
            <Smartphone className="w-4 h-4" />
            <span>1. Device Specifications &amp; Branding</span>
          </h3>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="md:col-span-2">
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                Device Model Name *
              </label>
              <input
                type="text"
                required
                value={productName}
                onChange={(e) => setProductName(e.target.value)}
                placeholder="e.g. iPhone 15 Pro Max, Galaxy S24 Ultra, MacBook Pro M3"
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-sasan-text font-semibold focus:outline-none focus:border-sasan-cyan"
              />
            </div>

            <div>
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                Brand
              </label>
              <select
                value={brand}
                onChange={(e) => setBrand(e.target.value)}
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-sasan-text font-semibold focus:outline-none focus:border-sasan-cyan"
              >
                <option value="Apple">Apple</option>
                <option value="Samsung">Samsung</option>
                <option value="Google">Google Pixel</option>
                <option value="OnePlus">OnePlus</option>
                <option value="Xiaomi">Xiaomi</option>
                <option value="Sony">Sony</option>
                <option value="Other">Other</option>
              </select>
            </div>

            <div>
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                Category
              </label>
              <select
                value={category}
                onChange={(e) => setCategory(e.target.value as DeviceType)}
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-sasan-text font-semibold focus:outline-none focus:border-sasan-cyan"
              >
                <option value="Smartphone">Smartphone</option>
                <option value="Tablet">Tablet / iPad</option>
                <option value="Laptop">Laptop / MacBook</option>
                <option value="Smartwatch">Smartwatch / Watch</option>
                <option value="Audio">Audio / AirPods</option>
                <option value="Accessory">Accessory</option>
              </select>
            </div>

            <div>
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                Storage Capacity
              </label>
              <input
                type="text"
                value={storageCapacity}
                onChange={(e) => setStorageCapacity(e.target.value)}
                placeholder="128GB, 256GB, 512GB, 1TB"
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-sasan-text font-mono focus:outline-none focus:border-sasan-cyan"
              />
            </div>

            <div>
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                Color
              </label>
              <input
                type="text"
                value={color}
                onChange={(e) => setColor(e.target.value)}
                placeholder="e.g. Natural Titanium, Onyx Black"
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-sasan-text focus:outline-none focus:border-sasan-cyan"
              />
            </div>
          </div>
        </div>

        {/* Section 2: Hardware Telemetry */}
        <div className="bg-sasan-surface border border-sasan-border rounded-xl p-5 space-y-4">
          <h3 className="text-xs font-bold uppercase tracking-wider text-sasan-gold flex items-center space-x-2">
            <ShieldCheck className="w-4 h-4" />
            <span>2. Hardware Telemetry &amp; Serial Identity</span>
          </h3>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                Primary IMEI 1 *
              </label>
              <input
                type="text"
                required
                maxLength={15}
                value={imei1}
                onChange={(e) => setImei1(e.target.value)}
                placeholder="15-digit IMEI barcode scan"
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-cyan-300 font-mono font-bold focus:outline-none focus:border-sasan-cyan"
              />
              <span className="text-[10px] text-sasan-muted mt-0.5 block font-mono">
                Digits: {imei1.length} / 15
              </span>
            </div>

            <div>
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                Secondary IMEI 2 (eSIM)
              </label>
              <input
                type="text"
                maxLength={15}
                value={imei2}
                onChange={(e) => setImei2(e.target.value)}
                placeholder="Optional secondary IMEI"
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-sasan-text font-mono focus:outline-none focus:border-sasan-cyan"
              />
            </div>

            <div>
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                Serial Number
              </label>
              <input
                type="text"
                value={serialNumber}
                onChange={(e) => setSerialNumber(e.target.value)}
                placeholder="Apple / OEM Serial Number"
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-sasan-text font-mono focus:outline-none focus:border-sasan-cyan"
              />
            </div>

            <div>
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                Condition Grade
              </label>
              <select
                value={conditionGrade}
                onChange={(e) => setConditionGrade(e.target.value as ConditionGrade)}
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-amber-300 font-bold focus:outline-none focus:border-sasan-cyan"
              >
                <option value="BrandNew">Brand New Sealed</option>
                <option value="LikeNew">Like New (Mint 10/10)</option>
                <option value="GradeA_Excellent">Grade A (Excellent)</option>
                <option value="GradeB_Good">Grade B (Minor Scuffs)</option>
                <option value="GradeC_Fair">Grade C (Wear &amp; Tear)</option>
                <option value="Refurbished">Refurbished / Serviced</option>
              </select>
            </div>

            <div>
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                Battery Health (%)
              </label>
              <input
                type="number"
                min={50}
                max={100}
                value={batteryHealth}
                onChange={(e) => setBatteryHealth(parseInt(e.target.value) || 100)}
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-emerald-400 font-mono font-bold focus:outline-none focus:border-sasan-cyan"
              />
            </div>
          </div>
        </div>

        {/* Section 3: Cost, Pricing & GST Rule 32(5) */}
        <div className="bg-sasan-surface border border-sasan-border rounded-xl p-5 space-y-4">
          <h3 className="text-xs font-bold uppercase tracking-wider text-emerald-400 flex items-center space-x-2">
            <Sparkles className="w-4 h-4" />
            <span>3. Purchase Cost, Tagged Price &amp; Rule 32(5) Margin Intelligence</span>
          </h3>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                Purchase Cost (₹)
              </label>
              <input
                type="number"
                required
                value={purchaseCost}
                onChange={(e) => setPurchaseCost(parseFloat(e.target.value) || 0)}
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-sasan-text font-mono font-bold focus:outline-none focus:border-sasan-cyan"
              />
            </div>

            <div>
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                Tagged Selling Price (₹)
              </label>
              <input
                type="number"
                required
                value={taggedPrice}
                onChange={(e) => setTaggedPrice(parseFloat(e.target.value) || 0)}
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-emerald-400 font-mono font-bold focus:outline-none focus:border-sasan-cyan"
              />
            </div>

            {/* Projected Margin Card */}
            <div className="bg-emerald-950/30 border border-emerald-800/40 rounded-xl p-3 flex flex-col justify-between">
              <span className="text-[10px] uppercase font-bold text-emerald-400 tracking-wider">
                Rule 32(5) Projected Margin
              </span>
              <div className="flex items-baseline justify-between mt-1">
                <span className="text-lg font-mono font-black text-emerald-300">
                  ₹{projectedProfit.toLocaleString('en-IN')}
                </span>
                <span className="text-[10px] font-mono text-emerald-400 font-bold">
                  GST: ₹{estimatedMarginGst.toLocaleString('en-IN', { maximumFractionDigits: 0 })}
                </span>
              </div>
            </div>
          </div>
        </div>

        {/* Submit Action */}
        <div className="flex items-center justify-end space-x-4 pt-2">
          <button
            type="submit"
            disabled={isSubmitting}
            className="inline-flex items-center space-x-2 px-8 py-3 rounded-xl text-xs font-black uppercase tracking-wider bg-sasan-cyan hover:bg-cyan-400 text-slate-950 shadow-lg shadow-cyan-950/60 transition-all cursor-pointer"
          >
            <Printer className="w-4 h-4" />
            <span>
              {isSubmitting ? 'Taking Into Stock...' : 'Take Into Stock & Spool Barcode [F5]'}
            </span>
          </button>
        </div>
      </form>

      {/* Barcode Sticker Modal on Success */}
      <BarcodeStickerPrintModal
        isOpen={isPrintModalOpen}
        onClose={() => setIsPrintModalOpen(false)}
        unit={intakeSuccessUnit}
      />
    </div>
  );
};
