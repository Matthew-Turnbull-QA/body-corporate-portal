import { useAuth } from "../features/auth/AuthContext";

export function HomePage() {
  const { user } = useAuth();

  return (
    <p>
      Signed in as {user?.email}. Job management will land here as later features are built.
    </p>
  );
}
