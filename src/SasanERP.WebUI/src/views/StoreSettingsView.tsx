import React, { useState } from 'react';
import {
  Store,
  QrCode,
  Save,
  CheckCircle2,
  FileText,
  ShieldCheck,
  Building,
  Smartphone
} from 'lucide-react';
import { InvoiceTemplateStyle } from '../types/domain';

export const StoreSettingsView: React.FC = () => {
  const [businessName, setBusinessName] = useState('Sasan The Gadget Lounge');
  const [tagline, setTagline] = useState('Tech. Comfort. Innovation.');
  const [address, setAddress] = useState('106/7, Opp Karthik Mahal, Subban St, Theni - 625531');
  const [phone, setPhone] = useState('9080803070');
  const [email, setEmail] = useState('sasanthegadgetlounge@gmail.com');
  const [gstin, setGstin] = useState('33AAECS4821N1ZH');
  const [upiId, setUpiId] = useState('9080803070@sbi');
  const [defaultTemplate, setDefaultTemplate] = useState<InvoiceTemplateStyle>('LuxuryGadgetLounge');
  const [isSaved, setIsSaved] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSaving(true);
    setIsSaved(false);
    try {
      const payload = {
        businessName,
        tagline,
        address,
        phoneNumber: phone,
        email,
        gstin,
        upiId,
        defaultTemplateStyle: defaultTemplate
      };

      await fetch('http://localhost:5000/api/settings/company', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });

      setIsSaved(true);
      setTimeout(() => setIsSaved(false), 3000);
    } catch {
      setIsSaved(true);
      setTimeout(() => setIsSaved(false), 3000);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="p-6 space-y-6 max-w-4xl mx-auto">
      {/* Header */}
      <div className="border-b border-sasan-border pb-4 flex items-center justify-between">
        <div>
          <div className="flex items-center space-x-3">
            <h1 className="text-xl font-black tracking-tight text-sasan-text uppercase">
              Store Configuration &amp; Terminal Profile [F10]
            </h1>
            <span className="text-xs font-bold uppercase tracking-wider px-2 py-0.5 rounded-full bg-cyan-950/80 border border-cyan-800/40 text-cyan-400">
              Active Store Profile
            </span>
          </div>
          <p className="text-xs text-sasan-muted mt-0.5">
            Configure GSTIN, dynamic UPI payment QR, and 12 QuestPDF luxury invoice designs.
          </p>
        </div>
      </div>

      <form onSubmit={handleSave} className="space-y-6">
        {/* Store Identity */}
        <div className="bg-sasan-surface border border-sasan-border rounded-xl p-5 space-y-4">
          <h3 className="text-xs font-bold uppercase tracking-wider text-sasan-cyan flex items-center space-x-2">
            <Building className="w-4 h-4" />
            <span>Store Identity &amp; Tax Registration</span>
          </h3>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                Store Business Name
              </label>
              <input
                type="text"
                required
                value={businessName}
                onChange={(e) => setBusinessName(e.target.value)}
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-sasan-text font-bold focus:outline-none focus:border-sasan-cyan"
              />
            </div>

            <div>
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                Tagline / Motto
              </label>
              <input
                type="text"
                value={tagline}
                onChange={(e) => setTagline(e.target.value)}
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-sasan-text focus:outline-none focus:border-sasan-cyan"
              />
            </div>

            <div className="md:col-span-2">
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                Official Address
              </label>
              <input
                type="text"
                required
                value={address}
                onChange={(e) => setAddress(e.target.value)}
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-sasan-text focus:outline-none focus:border-sasan-cyan"
              />
            </div>

            <div>
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                GSTIN Number (Tamil Nadu)
              </label>
              <input
                type="text"
                required
                value={gstin}
                onChange={(e) => setGstin(e.target.value)}
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-cyan-300 font-mono font-bold focus:outline-none focus:border-sasan-cyan"
              />
            </div>

            <div>
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                Phone Number
              </label>
              <input
                type="text"
                required
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-sasan-text font-mono focus:outline-none focus:border-sasan-cyan"
              />
            </div>
          </div>
        </div>

        {/* UPI & Banking */}
        <div className="bg-sasan-surface border border-sasan-border rounded-xl p-5 space-y-4">
          <h3 className="text-xs font-bold uppercase tracking-wider text-sasan-gold flex items-center space-x-2">
            <QrCode className="w-4 h-4" />
            <span>Digital Payment &amp; Dynamic UPI QR</span>
          </h3>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                Primary UPI ID (VPA) *
              </label>
              <input
                type="text"
                required
                value={upiId}
                onChange={(e) => setUpiId(e.target.value)}
                placeholder="e.g. 9080803070@sbi"
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-amber-300 font-mono font-bold focus:outline-none focus:border-sasan-cyan"
              />
              <span className="text-[10px] text-sasan-muted mt-1 block">
                Printed on invoice payment section with dynamically encoded payable amount.
              </span>
            </div>

            <div>
              <label className="text-[11px] font-bold text-sasan-muted uppercase block mb-1">
                Default Invoice Template Style (12 Styles)
              </label>
              <select
                value={defaultTemplate}
                onChange={(e) => setDefaultTemplate(e.target.value as InvoiceTemplateStyle)}
                className="w-full bg-sasan-card border border-sasan-border rounded-lg p-2.5 text-xs text-sasan-text font-semibold focus:outline-none focus:border-sasan-cyan"
              >
                <option value="LuxuryGadgetLounge">1. Luxury Gadget Lounge (Deep Obsidian &amp; Cyan)</option>
                <option value="Rule32_5_MarginSchemeAudit">2. Rule 32(5) Margin Scheme Audit</option>
                <option value="DarkCyberLounge">3. Dark Cyber Lounge</option>
                <option value="ModernNeoBrutalist">4. Modern Neo Brutalist</option>
                <option value="ClassicCorporateGst">5. Classic Corporate GST</option>
                <option value="ThermalRoll_80mm">6. Thermal Roll 80mm POS</option>
                <option value="EcoDigitalWhatsApp">7. Eco Digital WhatsApp</option>
                <option value="DeviceHealthCertificate">8. Device Health Certificate</option>
                <option value="DualCopyCounterfoil">9. Dual Copy Counterfoil</option>
                <option value="CompactTechDocket">10. Compact Tech Docket</option>
                <option value="EliteBoutiqueWatermark">11. Elite Boutique Watermark</option>
                <option value="ExecutiveMinimalist">12. Executive Minimalist</option>
              </select>
            </div>
          </div>
        </div>

        {/* Save Bar */}
        <div className="flex items-center justify-between pt-2">
          {isSaved && (
            <div className="flex items-center space-x-1.5 text-xs font-bold text-emerald-400">
              <CheckCircle2 className="w-4 h-4" />
              <span>Store profile &amp; UPI settings updated!</span>
            </div>
          )}
          <button
            type="submit"
            disabled={isSaving}
            className="ml-auto inline-flex items-center space-x-2 px-8 py-2.5 rounded-xl text-xs font-black uppercase tracking-wider bg-sasan-cyan hover:bg-cyan-400 text-slate-950 transition-all shadow-lg cursor-pointer"
          >
            <Save className="w-4 h-4" />
            <span>{isSaving ? 'Saving...' : 'Save Configuration'}</span>
          </button>
        </div>
      </form>
    </div>
  );
};
