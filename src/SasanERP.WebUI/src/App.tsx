import React, { useState } from 'react';
import { Shell } from './components/layout/Shell';
import { ModuleKey } from './components/layout/SidebarRail';
import { DashboardView } from './views/DashboardView';
import { PosBillingView } from './views/PosBillingView';
import { StockRegistryView } from './views/StockRegistryView';
import { DeviceIntakeView } from './views/DeviceIntakeView';
import { DaybookExpensesView } from './views/DaybookExpensesView';
import { GstReportsView } from './views/GstReportsView';
import { StoreSettingsView } from './views/StoreSettingsView';
import { GodModeView } from './views/GodModeView';

export default function App() {
  const [activeModule, setActiveModule] = useState<ModuleKey>('dashboard');

  const renderModuleView = () => {
    switch (activeModule) {
      case 'dashboard':
        return <DashboardView onNavigate={(m) => setActiveModule(m as ModuleKey)} />;
      case 'pos':
        return <PosBillingView onNavigate={(m) => setActiveModule(m as ModuleKey)} />;
      case 'stock':
        return <StockRegistryView onNavigateToIntake={() => setActiveModule('intake')} />;
      case 'intake':
        return <DeviceIntakeView />;
      case 'daybook':
        return <DaybookExpensesView />;
      case 'gst':
        return <GstReportsView />;
      case 'settings':
        return <StoreSettingsView />;
      case 'godmode':
        return <GodModeView />;
      default:
        return (
          <div className="p-8">
            <div className="bg-sasan-surface border border-sasan-border rounded-xl p-6 shadow-sm">
              <h2 className="text-xl font-bold text-sasan-cyan uppercase tracking-wide">
                Module: {activeModule.toUpperCase()}
              </h2>
              <p className="text-xs text-sasan-muted mt-1">
                Press F1-F12 to switch modules smoothly with zero latency.
              </p>
            </div>
          </div>
        );
    }
  };

  return (
    <Shell activeModule={activeModule} onSelectModule={setActiveModule}>
      {renderModuleView()}
    </Shell>
  );
}
