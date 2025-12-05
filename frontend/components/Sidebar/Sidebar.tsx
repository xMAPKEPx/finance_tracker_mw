'use client';

import Link from 'next/link';
import Image from 'next/image';
import { usePathname } from 'next/navigation';
import { useState } from 'react';

interface SidebarProps {
  className?: string;
  onMouseEnter?: () => void;
  onMouseLeave?: () => void;
}

const Sidebar = ({ className = '', onMouseEnter, onMouseLeave }: SidebarProps) => {
  const pathname = usePathname();
  const [mouseIn, SetMouseIn] = useState(false);

  const handleMouseEnter = () => {
    SetMouseIn(true);
    onMouseEnter?.();
  };

  const handleMouseLeave = () => {
    SetMouseIn(false);
    onMouseLeave?.();
  };

  const navigationItems = [
    {
      name: 'Дашборд',
      href: '/dashboard',
      icon: '/icons/home.png',
    },
    {
      name: 'Транзакции',
      href: '/transactions',
      icon: '/icons/active.png',
    },
    {
      name: 'Загрузить чек',
      href: '/qr-upload',
      icon: '/icons/requests.png',
    },
    {
      name: 'Счета и кредитки',
      href: '/accountsAndLoans',
      icon: '/icons/archive.png',
    },
  ];

  const bottomNavigationItems = [
    {
      name: 'Настройки',
      href: '/settings',
      icon: '/icons/settings.png',
    },
  ];

  return (
    <aside 
      className={`bg-white border-r border-gray-300 h-screen flex flex-col sidebar-transition fixed left-0 top-0 z-10 ${
        mouseIn ? 'w-[18.75rem]' : 'w-[7.188rem]'
      } ${className}`}
      onMouseEnter={handleMouseEnter} 
      onMouseLeave={handleMouseLeave}
    >

      {/* Навигационное меню */}
      <nav className="flex-1 p-4">
        <ul className="space-y-2">
          {navigationItems.map((item) => {
            const isActive = pathname === item.href || 
              (item.href === '/active' && pathname.startsWith('/projects'));
            return (
              <li key={item.href}>
                <Link
                  href={item.href}
                  className={`flex items-center rounded-lg transition-colors ${
                    mouseIn ? 'px-4' : 'px-2 justify-center'
                  } py-3 ${
                    isActive
                      ? 'bg-blue-100 text-blue-700 border-r-2 border-blue-700'
                      : 'text-gray-700 hover:bg-gray-100 hover:text-gray-900'
                  }`}
                >
                  {/* <span className={`flex-shrink-0 ${mouseIn ? 'mr-3' : 'mr-0'}`}>
                    <Image
                      src={item.icon}
                      alt={item.name}
                      width={50}
                      height={50}
                      className="w-8 filter invert-0"
                    />
                  </span> */}
                  <span className={`font-medium sidebar-transition overflow-hidden whitespace-nowrap ${
                    mouseIn ? 'opacity-100 max-w-xs' : 'opacity-0 max-w-0'
                  }`}>
                    {item.name}
                  </span>
                </Link>
              </li>
            );
          })}
        </ul>
      </nav>

      {/* Нижняя часть с настройками */}
      <div className="p-4 border-t border-gray-300">
        <ul className="space-y-2">
          {bottomNavigationItems.map((item) => {
            const isActive = pathname === item.href;
            return (
              <li key={item.href}>
                <Link
                  href={item.href}
                  className={`flex items-center rounded-lg transition-colors ${
                    mouseIn ? 'px-4' : 'px-2 justify-center'
                  } py-3 ${
                    isActive
                      ? 'bg-blue-100 text-blue-700 border-r-2 border-blue-700'
                      : 'text-gray-700 hover:bg-gray-100 hover:text-gray-900'
                  }`}
                >
                  {/* <span className={`flex-shrink-0 ${mouseIn ? 'mr-3' : 'mr-0'}`}>
                    <Image
                      src={item.icon}
                      alt={item.name}
                      width={20}
                      height={20}
                      className="w-5 h-5 filter invert-0"
                    />
                  </span> */}
                  <span className={`font-medium sidebar-transition overflow-hidden whitespace-nowrap ${
                    mouseIn ? 'opacity-100 max-w-xs' : 'opacity-0 max-w-0'
                  }`}>
                    {item.name}
                  </span>
                </Link>
              </li>
            );
          })}
        </ul>
      </div>
    </aside>
  );
};

export default Sidebar;