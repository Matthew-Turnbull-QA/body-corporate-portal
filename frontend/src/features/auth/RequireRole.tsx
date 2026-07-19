import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "./AuthContext";
import type { UserRole } from "./types";

export function RequireRole({ role }: { role: UserRole }) {
  const { user } = useAuth();

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  if (user.role !== role) {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
}
