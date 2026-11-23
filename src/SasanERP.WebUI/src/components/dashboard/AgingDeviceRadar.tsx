import React from 'react';
import { Smartphone, Sparkles, Clock, AlertTriangle } from 'lucide-react';
import { AgingDevice } from '../../types/domain';

interface AgingDeviceRadarProps {
  devices: AgingDevice[];
  onDeviceClick?: (imei: string) => void;
}

export const AgingDeviceRadar: React.FC<AgingDeviceRadarProps> = ({
  devices,
  onDeviceClick
}) => {
  if (devices.length === 0) {
    return (
      <div className="bg-emerald-950/20 border border-emerald-600/30 rounded-xl p-8 flex flex-col items-center justify-center text-center">
        <div className="w-12 h-12 rounded-full bg-emerald-900/40 border border-emerald-500/30 flex items-center justify-center mb-3">
          <Sparkles className="w-6 h-6 text-emerald-400" />
        </div>
        <h4 className="font-bold text-sm text-emerald-300">100% Fresh Inventory Turnover</h4>
        <p className="text-xs text-emerald-500/80 mt-1 max-w-sm">
          Zero pre-owned devices have exceeded the 60-day turnover ceiling. Capital rotation is in peak health.
        </p>
        <span className="mt-3 text-[10px] uppercase font-bold tracking-wider px-2.5 py-1 rounded-full bg-emerald-950/60 border border-emerald-700/50 text-emerald-300">
          Zero Capital Locked
        </span>
      </div>
    );
  }

  return (
    <div className="overflow-x-auto rounded-lg border border-sasan-border">
      <table className="w-full text-left text-xs">
        <thead className="bg-sasan-surface text-sasan-muted uppercase text-[10px] tracking-wider border-b border-sasan-border">
          <tr>
            <th className="py-2.5 px-3">Device</th>
            <th className="py-2.5 px-3">IMEI</th>
            <th className="py-2.5 px-3">Aging</th>
            <th className="py-2.5 px-3 text-right">Price</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-sasan-border bg-sasan-card">
          {devices.map((device) => {
            const isCritical = device.agingDays >= 90;
            return (
              <tr
                key={device.id}
                onClick={() => onDeviceClick && onDeviceClick(device.imei1)}
                className="hover:bg-slate-800/40 transition-colors cursor-pointer"
              >
                <td className="py-2.5 px-3 font-semibold text-sasan-text flex items-center space-x-2">
                  <Smartphone className="w-3.5 h-3.5 text-sasan-cyan flex-shrink-0" />
                  <span>{device.deviceName}</span>
                </td>
                <td className="py-2.5 px-3 font-mono text-[11px] text-cyan-300">
                  {device.imei1}
                </td>
                <td className="py-2.5 px-3">
                  <span
                    className={`inline-flex items-center space-x-1 px-2 py-0.5 rounded text-[11px] font-bold border ${
                      isCritical
                        ? 'bg-rose-950/60 border-rose-800/50 text-rose-300'
                        : 'bg-amber-950/60 border-amber-800/50 text-amber-300'
                    }`}
                  >
                    {isCritical ? (
                      <AlertTriangle className="w-3 h-3 text-rose-400" />
                    ) : (
                      <Clock className="w-3 h-3 text-amber-400" />
                    )}
                    <span>{device.agingDays}d</span>
                  </span>
                </td>
                <td className="py-2.5 px-3 text-right font-mono font-bold text-emerald-400">
                  ₹{device.taggedPrice.toLocaleString('en-IN')}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
};
