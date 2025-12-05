/**
 * Конфигурация API
 * Используйте переменные окружения для настройки URL API
 */
export const API_CONFIG = {
  baseURL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:3001/api',
  timeout: 30000, // 30 секунд
  useMockData: process.env.NEXT_PUBLIC_USE_MOCK_DATA === 'true' || !process.env.NEXT_PUBLIC_API_URL,
} as const;

/**
 * Endpoints API
 */
export const API_ENDPOINTS = {
  // Auth
  auth: {
    login: '/auth/login',
    logout: '/auth/logout',
    refresh: '/auth/refresh',
    me: '/auth/me',
  },
  
} as const;

