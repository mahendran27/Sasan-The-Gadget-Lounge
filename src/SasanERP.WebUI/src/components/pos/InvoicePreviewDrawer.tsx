import React from 'react';
import { X, Printer, Download, Share2, CheckCircle2, RotateCcw } from 'lucide-react';

interface InvoicePreviewDrawerProps {
  isOpen: boolean;
  onClose: () => void;
  invoiceId?: number;
  invoiceNumber?: string;
  onStartNewSale: () => void;
}

export const InvoicePreviewDrawer: React.FC<InvoicePreviewDrawerProps> = ({
  isOpen,
  onClose,
  invoiceId,
  invoiceNumber,
  onStartNewSale
}) => {
  if (!isOpen) return null;

  const pdfUrl = invoiceId
    ? `http://localhost:5000/api/print/invoice-pdf/${invoiceId}`
    : '';

  const handlePrint = () => {
    const iframe = document.getElementById('invoice-pdf-iframe') as HTMLIFrameElement;
    iframe?.contentWindow?.print();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 backdrop-blur-sm p-4">
      <div className="bg-sasan-surface border border-sasan-border rounded-2xl w-full max-w-4xl h-[90vh] shadow-2xl overflow-hidden flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-sasan-border bg-sasan-card">
          <div className="flex items-center space-x-2">
            <CheckCircle2 className="w-5 h-5 text-emerald-400" />
            <div>
              <h3 className="font-bold text-sm text-sasan-text uppercase tracking-wider">
                Invoice Generated: {invoiceNumber || 'SGL/26-27/0001'}
              </h3>
              <p className="text-[11px] text-emerald-400 font-semibold">
                ACID Ledger Committed • Section 32(5) Compliant • QuestPDF High Fidelity
              </p>
            </div>
          </div>
          <div className="flex items-center space-x-2">
            <button
              onClick={handlePrint}
              className="inline-flex items-center space-x-1 px-3 py-1.5 rounded-lg text-xs font-bold bg-sasan-cyan hover:bg-cyan-400 text-slate-950 transition-colors shadow-sm"
            >
              <Printer className="w-3.5 h-3.5" />
              <span>Print [F9]</span>
            </button>
            <a
              href={pdfUrl}
              download={`${invoiceNumber || 'Invoice'}.pdf`}
              className="inline-flex items-center space-x-1 px-3 py-1.5 rounded-lg text-xs font-bold bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700 transition-colors"
            >
              <Download className="w-3.5 h-3.5" />
              <span>Download</span>
            </a>
            <button
              onClick={onClose}
              className="p-1 rounded-lg text-sasan-muted hover:text-sasan-text hover:bg-slate-800 transition-colors"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* PDF Viewer Body */}
        <div className="flex-1 bg-slate-900 p-2 overflow-hidden flex items-center justify-center">
          {pdfUrl ? (
            <iframe
              id="invoice-pdf-iframe"
              src={pdfUrl}
              title="Invoice PDF Preview"
              className="w-full h-full rounded-lg border border-slate-800 bg-white"
            />
          ) : (
            <div className="text-center p-8 text-sasan-muted text-xs">
              Preview generation active...
            </div>
          )}
        </div>

        {/* Footer Actions */}
        <div className="p-3 border-t border-sasan-border bg-sasan-card flex items-center justify-between">
          <span className="text-xs text-sasan-muted">
            Printed via direct thermal or laser A4 vector pipeline.
          </span>
          <button
            onClick={onStartNewSale}
            className="inline-flex items-center space-x-1.5 px-4 py-2 rounded-xl text-xs font-black bg-sky-600 hover:bg-sky-500 text-white transition-colors"
          >
            <RotateCcw className="w-3.5 h-3.5" />
            <span>Start Fresh Bill [F2]</span>
          </button>
        </div>
      </div>
    </div>
  );
};
