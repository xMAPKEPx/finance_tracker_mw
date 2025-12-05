import { API_CONFIG } from '@/config/api';

/**
 * Типы для API ответов
 */
export interface ApiResponse<T> {
  data: T;
  message?: string;
  errors?: string[];
}

export interface ApiError {
  message: string;
  status: number;
  errors?: Record<string, string[]>;
}

/**
 * Класс для работы с API
 */
class ApiClient {
  private baseURL: string;
  private timeout: number;

  constructor() {
    this.baseURL = API_CONFIG.baseURL;
    this.timeout = API_CONFIG.timeout;
  }

  /**
   * Получить токен авторизации из store
   * В клиентском компоненте можно передать токен напрямую через параметры
   */
  private getAuthToken(customToken?: string): string | null {
    if (customToken) return customToken;
    
    if (typeof window === 'undefined') return null;
    
    try {
      const state = JSON.parse(localStorage.getItem('persist:auth') || '{}');
      const auth = JSON.parse(state.auth || '{}');
      return auth.accessToken || null;
    } catch {
      return null;
    }
  }

  /**
   * Создать заголовки запроса
   */
  private getHeaders(customHeaders?: HeadersInit, customToken?: string): HeadersInit {
    const token = this.getAuthToken(customToken);

    // Use Headers instance to safely set headers regardless of incoming HeadersInit shape
    const headers = new Headers();
    headers.set('Content-Type', 'application/json');

    if (customHeaders) {
      // customHeaders can be Headers, [string, string][], or Record<string, string>
      if (customHeaders instanceof Headers) {
        customHeaders.forEach((value, key) => headers.set(key, value));
      } else if (Array.isArray(customHeaders)) {
        customHeaders.forEach(([key, value]) => headers.set(key, value));
      } else {
        Object.entries(customHeaders).forEach(([key, value]) => headers.set(key, value as string));
      }
    }

    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
    }

    return headers;
  }

  /**
   * Обработка ошибок
   */
  private async handleResponse<T>(response: Response): Promise<T> {
    if (!response.ok) {
      let errorMessage = `HTTP error! status: ${response.status}`;
      let errors: Record<string, string[]> | undefined;

      try {
        const errorData = await response.json();
        errorMessage = errorData.message || errorData.error || errorMessage;
        errors = errorData.errors;
      } catch {
        // Если не удалось распарсить JSON, используем текст ответа
        try {
          errorMessage = await response.text();
        } catch {
          // Оставляем дефолтное сообщение
        }
      }

      const error: ApiError = {
        message: errorMessage,
        status: response.status,
        errors,
      };

      throw error;
    }

    // Если ответ пустой (например, при DELETE)
    if (response.status === 204 || response.headers.get('content-length') === '0') {
      return {} as T;
    }

    const contentType = response.headers.get('content-type');
    if (contentType && contentType.includes('application/json')) {
      const data = await response.json();
      // Если ответ обернут в { data: ... }, извлекаем data
      return (data.data !== undefined ? data.data : data) as T;
    }

    return (await response.text()) as unknown as T;
  }

  /**
   * Выполнить запрос с таймаутом
   */
  private async fetchWithTimeout(
    url: string,
    options: RequestInit
  ): Promise<Response> {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), this.timeout);

    try {
      const response = await fetch(url, {
        ...options,
        signal: controller.signal,
      });
      clearTimeout(timeoutId);
      return response;
    } catch (error) {
      clearTimeout(timeoutId);
      if (error instanceof Error && error.name === 'AbortError') {
        throw new Error('Request timeout');
      }
      throw error;
    }
  }

  /**
   * Базовый метод для выполнения запросов
   */
  private async request<T>(
    endpoint: string,
    options: RequestInit & { token?: string } = {}
  ): Promise<T> {
    const url = `${this.baseURL}${endpoint}`;
    const { token, ...requestOptions } = options;
    
    const config: RequestInit = {
      ...requestOptions,
      headers: this.getHeaders(requestOptions.headers, token),
    };

    try {
      const response = await this.fetchWithTimeout(url, config);
      return await this.handleResponse<T>(response);
    } catch (error) {
      if (error instanceof Error) {
        throw {
          message: error.message,
          status: 0,
        } as ApiError;
      }
      throw error;
    }
  }

  /**
   * GET запрос
   */
  async get<T>(endpoint: string, options?: RequestInit): Promise<T> {
    return this.request<T>(endpoint, {
      ...options,
      method: 'GET',
    });
  }

  /**
   * POST запрос
   */
  async post<T>(endpoint: string, data?: unknown, options?: RequestInit): Promise<T> {
    return this.request<T>(endpoint, {
      ...options,
      method: 'POST',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  /**
   * PUT запрос
   */
  async put<T>(endpoint: string, data?: unknown, options?: RequestInit): Promise<T> {
    return this.request<T>(endpoint, {
      ...options,
      method: 'PUT',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  /**
   * PATCH запрос
   */
  async patch<T>(endpoint: string, data?: unknown, options?: RequestInit): Promise<T> {
    return this.request<T>(endpoint, {
      ...options,
      method: 'PATCH',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  /**
   * DELETE запрос
   */
  async delete<T>(endpoint: string, options?: RequestInit): Promise<T> {
    return this.request<T>(endpoint, {
      ...options,
      method: 'DELETE',
    });
  }
}

// Экспортируем singleton экземпляр
export const apiClient = new ApiClient();

