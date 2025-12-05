/**
 * Интерфейсы для системы аутентификации
 */

export interface UserProfile {
  id: string;
  displayName: string;
  email: string;
}

export interface Credentials {
  username: string;
  password: string;
}

export interface AuthState {
  isAuthenticated: boolean;
  status: 'idle' | 'loading' | 'succeeded' | 'failed';
  error?: string;
  accessToken?: string;
  user?: UserProfile;
}