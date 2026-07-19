import { NavLink, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../features/auth/AuthContext";

function getInitials(value: string | null | undefined) {
  if (!value) {
    return "U";
  }

  const parts = value.split(/\s+/).filter(Boolean);
  if (parts.length === 0) {
    return "U";
  }

  return parts.slice(0, 2).map((part) => part[0]?.toUpperCase() ?? "").join("");
}

export function AppLayout() {
  const { user, signOut } = useAuth();
  const location = useLocation();
  const isLoginPage = location.pathname === "/login";

  if (!user) {
    return isLoginPage ? <Outlet /> : <div className="app-shell"><main className="app-shell__content"><Outlet /></main></div>;
  }

  return (
    <div className="app-shell">
      <header className="app-shell__header">
        <div className="app-shell__brand">
          <div className="app-shell__logo">R</div>
          <span className="app-shell__brand-name">Rietvlei Body Corp</span>
          <nav className="app-shell__nav" aria-label="Primary">
            <NavLink className={({ isActive }) => `app-shell__nav-link ${isActive ? "active" : ""}`} to="/properties">
              Properties
            </NavLink>
            {user.role === "Administrator" && (
              <NavLink className={({ isActive }) => `app-shell__nav-link ${isActive ? "active" : ""}`} to="/users">
                Users
              </NavLink>
            )}
          </nav>
        </div>

        <div className="app-shell__user">
          <div className="app-shell__avatar">{getInitials(user.displayName)}</div>
          <div className="app-shell__user-details">
            <span className="app-shell__user-name">{user.displayName}</span>
            <span className="app-shell__user-role">{user.role}</span>
          </div>
          <button className="button button--ghost" type="button" onClick={signOut}>
            Sign out
          </button>
        </div>
      </header>
      <main className="app-shell__content page-shell">
        <Outlet />
      </main>
    </div>
  );
}
