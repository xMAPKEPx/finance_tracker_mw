import { API_CONFIG } from '@/config/api';

/**
 * Утилиты для работы с API
 */

/**
 * Проверка, нужно ли использовать моковые данные
 */
export function shouldUseMockData(): boolean {
  return API_CONFIG.useMockData;
}

/**
 * Обертка для переключения между моками и API
 */
export async function withApiFallback<T>(
  apiCall: () => Promise<T>,
  mockCall: () => Promise<T>
): Promise<T> {
  if (shouldUseMockData()) {
    return mockCall();
  }
  
  try {
    return await apiCall();
  } catch (error) {
    console.warn('API call failed, falling back to mock data:', error);
    return mockCall();
  }
}

/**
 * Форматирование даты для API
 */
export function formatDateForApi(date: Date | string): string {
  const d = typeof date === 'string' ? new Date(date) : date;
  return d.toISOString();
}

/**
 * Парсинг даты из API
 */
export function parseDateFromApi(dateString: string): Date {
  return new Date(dateString);
}

/**
 * Обработка ошибок API
 */
export function handleApiError(error: unknown): string {
  if (error && typeof error === 'object' && 'message' in error) {
    return String(error.message);
  }
  return 'Произошла неизвестная ошибка';
}

/**
 * Создание query строки из объекта
 */
export function buildQueryString(params: Record<string, unknown>): string {
  const searchParams = new URLSearchParams();
  
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null) {
      if (Array.isArray(value)) {
        value.forEach((item) => searchParams.append(key, String(item)));
      } else {
        searchParams.append(key, String(value));
      }
    }
  });
  
  const query = searchParams.toString();
  return query ? `?${query}` : '';
}

