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
    login: '/Auth/login',
    register: '/Auth/register',
    logout: '/Auth/logout',
    refresh: '/Auth/refresh',
    me: '/Auth/me',
  },
  // Categories
  categories: {
    list: '/Categories',
    create: '/Categories',
    delete: (id: string | number) => `/Categories/${id}`,
  },
  // Receipts
  receipts: {
    parse: '/Receipts/parse',
    getUserReceipts: '/Receipts/user/Receipts',
    debugRaw: '/Receipts/debug-raw',
  },
} as const;

