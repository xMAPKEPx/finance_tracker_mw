'use client';

import { useState } from 'react';
import Sidebar from './Sidebar/Sidebar';

interface AppLayoutProps {
  children: React.ReactNode;
}

const AppLayout = ({ children }: AppLayoutProps) => {
  const [isSidebarExpanded, setIsSidebarExpanded] = useState(false);

  return (
    <div className="h-screen flex">
      <Sidebar 
        onMouseEnter={() => setIsSidebarExpanded(true)}
        onMouseLeave={() => setIsSidebarExpanded(false)}
      />
      <main className={`flex-1 flex-1 overflow-y-auto sidebar-transition ${
        isSidebarExpanded ? 'ml-[18.75rem]' : 'ml-[7.188rem]'
      }`}>
        <div className="max-w-7xl mx-auto p-6">
          {children}
        </div>
      </main>
    </div>
  );
};

export default AppLayout;