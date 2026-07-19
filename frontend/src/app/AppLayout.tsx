import { Link, Outlet } from "react-router-dom";
import { useAuth } from "../features/auth/AuthContext";

export function AppLayout() {
  const { user, signOut } = useAuth();

  return (
    <div className="app-shell">
      <header className="app-shell__header">
        <h1>Body Corporate Management Portal</h1>
        {user && (
          <div className="app-shell__user">
            <Link to="/properties">Properties</Link>
            {user.role === "Administrator" && <Link to="/users">Users</Link>}
            <span>
              {user.displayName} ({user.role})
            </span>
            <button type="button" onClick={signOut}>
              Sign out
            </button>
          </div>
        )}
      </header>
      <main className="app-shell__content">
        <Outlet />
      </main>
    </div>
  );
}
