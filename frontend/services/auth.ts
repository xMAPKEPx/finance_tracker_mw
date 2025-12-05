import { Credentials, UserProfile } from "@/types/auth";
import { apiClient } from "@/lib/api-client";
import { API_ENDPOINTS } from "@/config/api";
import { withApiFallback, shouldUseMockData } from "@/lib/api-utils";

const mockUsers: Array<UserProfile & { username: string; password: string }> = [
  {
    id: "u1",
    displayName: "Куратор Банка",
    email: "user@urfu.ru",
    username: "mapkeeeeee",
    password: "pass123",
  },
  {
    id: "u2",
    displayName: "Димасик",
    email: "student@urfu.ru",
    username: "Woodzeii",
    password: "pass123",
  },
];

// ========== Mock функции ==========
export async function mockLogin(credentials: Credentials): Promise<{ token: string; user: UserProfile }>{
  await delay(300);
  
  // Отладочная информация
  console.log("Попытка входа:", credentials);
  console.log("Доступные пользователи:", mockUsers.map(u => ({ username: u.username, password: u.password })));
  
  const account = mockUsers.find(
    (u) => {
      // Нормализуем имя пользователя для сравнения (приводим к нижнему регистру)
      const normalizedInput = credentials.username.toLowerCase().trim();
      const normalizedUsername = u.username.toLowerCase();
      
      // Проверяем точное совпадение
      const usernameMatch = normalizedUsername === normalizedInput;
      const passwordMatch = u.password === credentials.password;
      
      console.log(`Проверка пользователя: ${u.username}, вход: "${credentials.username}", username match: ${usernameMatch}, password match: ${passwordMatch}`);
      
      return usernameMatch && passwordMatch;
    }
  );
  
  if (!account) {
    console.log("Пользователь не найден");
    throw new Error("Неверные учетные данные");
  }
  
  console.log("Вход успешен:", account.displayName);
  return { token: `mock-token-${account.id}`, user: stripSecrets(account) };
}

export async function mockLogout(): Promise<void> {
  await delay(100);
}

// ========== API функции ==========
async function apiLogin(credentials: Credentials): Promise<{ token: string; user: UserProfile }> {
  const response = await apiClient.post<{ token: string; user: UserProfile }>(
    API_ENDPOINTS.auth.login,
    credentials
  );
  return response;
}

async function apiLogout(): Promise<void> {
  await apiClient.post(API_ENDPOINTS.auth.logout);
}

async function apiGetMe(): Promise<UserProfile> {
  return await apiClient.get<UserProfile>(API_ENDPOINTS.auth.me);
}

// ========== Публичные функции (с автоматическим выбором моков/API) ==========
/**
 * Вход в систему
 * Автоматически использует API или моки в зависимости от конфигурации
 */
export async function login(credentials: Credentials): Promise<{ token: string; user: UserProfile }> {
  return withApiFallback(
    () => apiLogin(credentials),
    () => mockLogin(credentials)
  );
}

/**
 * Выход из системы
 * Автоматически использует API или моки в зависимости от конфигурации
 */
export async function logout(): Promise<void> {
  return withApiFallback(
    () => apiLogout(),
    () => mockLogout()
  );
}

/**
 * Получить текущего пользователя
 */
export async function getCurrentUser(): Promise<UserProfile> {
  if (shouldUseMockData()) {
    // Для моков возвращаем первого пользователя как пример
    const mockUser = mockUsers[0];
    return stripSecrets(mockUser);
  }
  
  return apiGetMe();
}

function stripSecrets(user: (UserProfile & { username: string; password: string })): UserProfile {
  const { username: _u, password: _p, ...safe } = user;
  return safe;
}

function delay(ms: number) {
  return new Promise((res) => setTimeout(res, ms));
}


