'use client';

import { useEffect } from 'react';
import { usePathname, useRouter } from 'next/navigation';
import { useAppSelector } from '@/store/hooks';
import AppLayout from './AppLayout';

interface ConditionalLayoutProps {
  children: React.ReactNode;
}

const ConditionalLayout = ({ children }: ConditionalLayoutProps) => {
  const pathname = usePathname();
  const router = useRouter();
  const { isAuthenticated, status } = useAppSelector((state) => state.auth);
  
  useEffect(() => {
    // Не проверяем авторизацию пока статус загрузки (во время входа)
    if (status === 'loading') return;
    
    const isAuthPage = pathname.startsWith('/login');
    
    // Если не авторизован и не на странице авторизации - перенаправить на /login
    if (!isAuthenticated && !isAuthPage) {
      router.push('/login');
      return;
    }
    
    // Если авторизован и на странице авторизации - перенаправить на главную
    if (isAuthenticated && isAuthPage) {
      router.push('/');
      return;
    }
  }, [isAuthenticated, pathname, router, status]);
  
  // Страницы без навигации
  const authPages = ['/login', '/register'];
  const shouldShowNavigation = !authPages.some(page => pathname.startsWith(page));

  if (shouldShowNavigation) {
    return <AppLayout>{children}</AppLayout>;
  }

  return <>{children}</>;
};

export default ConditionalLayout;
