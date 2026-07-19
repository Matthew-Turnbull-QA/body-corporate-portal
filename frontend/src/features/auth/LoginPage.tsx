import { GoogleLogin, type CredentialResponse } from "@react-oauth/google";
import { useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { ApiError } from "../../api/client";
import { useAuth } from "./AuthContext";

export function LoginPage() {
  const { signInWithGoogle } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [error, setError] = useState<string | null>(null);

  const redirectTo = (location.state as { from?: Location } | null)?.from?.pathname ?? "/";

  async function handleSuccess(credential: CredentialResponse) {
    if (!credential.credential) {
      setError("Google did not return a credential. Please try again.");
      return;
    }

    setError(null);
    try {
      await signInWithGoogle(credential.credential);
      navigate(redirectTo, { replace: true });
    } catch (err) {
      setError(
        err instanceof ApiError && err.status === 401
          ? "This Google account is not registered, or has been disabled. Contact your administrator."
          : "Sign-in failed. Please try again.",
      );
    }
  }

  return (
    <section>
      <h2>Sign in</h2>
      <GoogleLogin onSuccess={handleSuccess} onError={() => setError("Google sign-in failed. Please try again.")} />
      {error && <p role="alert">{error}</p>}
    </section>
  );
}
