import React from 'react';
import { LucideIcon } from 'lucide-react';

interface KpiMetricCardProps {
  title: string;
  value: string | number;
  subtitle?: string;
  icon: LucideIcon;
  variant?: 'cyan' | 'gold' | 'emerald' | 'rose' | 'default';
  isCurrency?: boolean;
}

export const KpiMetricCard: React.FC<KpiMetricCardProps> = ({
  title,
  value,
  subtitle,
  icon: Icon,
  variant = 'default',
  isCurrency = false
}) => {
  const variantStyles = {
    cyan: 'border-sasan-border hover:border-sasan-cyan/60 text-sasan-cyan',
    gold: 'border-sasan-border hover:border-sasan-gold/60 text-sasan-gold',
    emerald: 'border-sasan-border hover:border-emerald-500/60 text-emerald-400',
    rose: 'border-sasan-border hover:border-rose-500/60 text-rose-400',
    default: 'border-sasan-border hover:border-sasan-muted/60 text-sasan-text',
  };

  const formattedValue = isCurrency && typeof value === 'number'
    ? `₹${value.toLocaleString('en-IN')}`
    : value;

  return (
    <div className={`bg-sasan-surface border rounded-xl p-4 transition-all shadow-sm flex flex-col justify-between ${variantStyles[variant]}`}>
      <div className="flex items-center justify-between">
        <span className="text-[11px] font-bold uppercase tracking-wider text-sasan-muted">{title}</span>
        <div className="p-2 rounded-lg bg-sasan-card border border-sasan-border">
          <Icon className="w-4 h-4 text-inherit" />
        </div>
      </div>
      <div className="mt-2">
        <div className="text-2xl font-black font-mono tracking-tight text-sasan-text">
          {formattedValue}
        </div>
        {subtitle && (
          <p className="text-[11px] text-sasan-muted mt-0.5 truncate">{subtitle}</p>
        )}
      </div>
    </div>
  );
};
