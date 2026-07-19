import { useAuth } from "../features/auth/AuthContext";

export function HomePage() {
  const { user } = useAuth();

  const firstName = user?.displayName?.split(/\s+/)[0] ?? user?.email?.split("@")[0] ?? "there";

  return (
    <section className="page-card">
      <div className="page-card__eyebrow">Dashboard</div>
      <h2>Welcome back, {firstName}</h2>
      <p className="text-muted">Signed in as {user?.email} · {user?.role}</p>
      <div className="divider" />
      <div className="empty-state">
        <h3>Job & task management</h3>
        <p>This capability is coming in a future release.</p>
      </div>
    </section>
  );
}
