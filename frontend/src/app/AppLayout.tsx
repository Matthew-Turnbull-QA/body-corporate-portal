import { Outlet } from "react-router-dom";

export function AppLayout() {
  return (
    <div className="app-shell">
      <header className="app-shell__header">
        <h1>Body Corporate Management Portal</h1>
      </header>
      <main className="app-shell__content">
        <Outlet />
      </main>
    </div>
  );
}
