import React, { useState } from 'react';
import { Shell } from './components/layout/Shell';
import { ModuleKey } from './components/layout/SidebarRail';

export default function App() {
  const [activeModule, setActiveModule] = useState<ModuleKey>('dashboard');

  return (
    <Shell activeModule={activeModule} onSelectModule={setActiveModule}>
      <div className="bg-sasan-surface border border-sasan-border rounded-xl p-6 shadow-sm">
        <h2 className="text-xl font-bold text-sasan-cyan uppercase tracking-wide">
          Module: {activeModule.toUpperCase()}
        </h2>
        <p className="text-xs text-sasan-muted mt-1">
          Press F1-F12 to switch modules smoothly with zero latency.
        </p>
      </div>
    </Shell>
  );
}
