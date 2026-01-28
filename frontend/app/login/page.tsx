"use client";

import { useState } from "react";
import { useAppDispatch, useAppSelector } from "@/store/hooks";
import { login } from "@/store/slices/authSlice";
import { Credentials } from "@/types/auth";
import { useRouter } from "next/navigation";
import AuthMenu from "@/components/AuthMenu";

export default function LoginPage() {
  const dispatch = useAppDispatch();
  const router = useRouter();
  const authStatus = useAppSelector((state) => state.auth.status);
  const authError = useAppSelector((state) => state.auth.error);
  const isAuthenticated = useAppSelector((state) => state.auth.isAuthenticated);

  const [credentials, setCredentials] = useState<Credentials>({
    username: "",
    password: "",
  });

  // Если пользователь уже аутентифицирован, перенаправляем на дашборд
  if (isAuthenticated) {
    router.push("/dashboard");
    return null;
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await dispatch(login(credentials)).unwrap();
      router.push("/dashboard");
    } catch (error) {
      console.error("Ошибка входа:", error);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setCredentials((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  const isLoading = authStatus === "loading";

  return (
    <div className="min-h-screen flex flex-col bg-gray-50">
      <AuthMenu />
      <div className="flex-1 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-md w-full space-y-8">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            Вход в систему
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            CheckCheker
          </p>
        </div>
        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          <div className="rounded-md shadow-sm -space-y-px">
            <div>
              <label htmlFor="username" className="sr-only">
                Имя пользователя
              </label>
              <input
                id="username"
                name="username"
                type="text"
                required
                className="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-t-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm"
                placeholder="Имя пользователя"
                value={credentials.username}
                onChange={handleInputChange}
                disabled={isLoading}
              />
            </div>
            <div>
              <label htmlFor="password" className="sr-only">
                Пароль
              </label>
              <input
                id="password"
                name="password"
                type="password"
                required
                className="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-b-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm"
                placeholder="Пароль"
                value={credentials.password}
                onChange={handleInputChange}
                disabled={isLoading}
              />
            </div>
          </div>

          {authError && (
            <div className="rounded-md bg-red-50 p-4">
              <div className="text-sm text-red-700">{authError}</div>
            </div>
          )}

          <div>
            <button
              type="submit"
              disabled={isLoading}
              className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isLoading ? "Вход..." : "Войти"}
            </button>
          </div>

          <div className="text-center">
            <div className="text-sm text-gray-600">
              Тестовые аккаунты:
            </div>
            <div className="mt-2 text-xs text-gray-500 space-y-1">
              <div>Куратор: curator / pass123</div>
              <div>Студент: student / pass123</div>
            </div>
          </div>
        </form>
        </div>
      </div>
    </div>
  );
}