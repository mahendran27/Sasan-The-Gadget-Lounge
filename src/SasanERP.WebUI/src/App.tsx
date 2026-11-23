import React, { useState } from 'react';

export default function App() {
  const [activeTab, setActiveTab] = useState('dashboard');

  return (
    <div className="flex h-screen w-screen bg-[#080E1A] text-[#F8FAFC]">
      <div className="p-4">
        <h1 className="text-xl font-bold text-[#00E5FF]">Sasan The Gadget Lounge</h1>
        <p className="text-xs text-[#8EA4C1]">React 18 + Tailwind CSS Luxury Shell Initialized</p>
      </div>
    </div>
  );
}
