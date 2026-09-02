import { Navigate } from "react-router-dom";
import { useAuthStore } from "../store/authStore";
import type { ReactNode } from "react";

export function RequireAuth({ children, roles }: { children: ReactNode; roles?: string[] }) {
  const { isAuthenticated, role } = useAuthStore();
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  if (roles && role && !roles.includes(role)) {
    if (role === "Customer") return <Navigate to="/customer" replace />;
    if (role === "ShopOwner") return <Navigate to="/shop" replace />;
    if (role === "Admin") return <Navigate to="/admin" replace />;
    return <Navigate to="/" replace />;
  }
  if (roles && !role) return <Navigate to="/login" replace />;
  return <>{children}</>;
}

export function RedirectIfAuthed({ children }: { children: ReactNode }) {
  const { isAuthenticated, role } = useAuthStore();
  if (isAuthenticated) {
    if (role === "Admin") return <Navigate to="/admin" replace />;
    if (role === "ShopOwner") return <Navigate to="/shop" replace />;
    return <Navigate to="/customer" replace />;
  }
  return <>{children}</>;
}
