import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "./AuthContext";

export function RequirePortalAdmin() {
  const { user } = useAuth();

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  if (!user.isPortalAdmin) {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
}
