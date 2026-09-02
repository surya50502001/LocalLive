import { create } from "zustand";
import type { UserDto } from "../types";
import { loadUser, saveUser, clearAuth } from "../api/client";

interface AuthState {
  user: UserDto | null;
  isAuthenticated: boolean;
  role: string | null;
  setUser: (user: UserDto | null) => void;
  logout: () => void;
  hydrate: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticated: false,
  role: null,
  setUser: (user) =>
    set(() => {
      if (user) {
        saveUser(user);
        return { user, isAuthenticated: true, role: user.role };
      }
      clearAuth();
      return { user: null, isAuthenticated: false, role: null };
    }),
  logout: () =>
    set(() => {
      clearAuth();
      return { user: null, isAuthenticated: false, role: null };
    }),
  hydrate: () =>
    set(() => {
      const u = loadUser<UserDto>();
      if (!u) return { user: null, isAuthenticated: false, role: null };
      const at = localStorage.getItem("accessToken");
      if (!at) return { user: null, isAuthenticated: false, role: null };
      return { user: u, isAuthenticated: true, role: u.role };
    }),
}));
