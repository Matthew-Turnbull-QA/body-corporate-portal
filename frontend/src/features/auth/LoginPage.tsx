import { GoogleLogin, type CredentialResponse } from "@react-oauth/google";
import { useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { ApiError } from "../../api/client";
import { useAuth } from "./AuthContext";
import heroImage from "../../../../docs/design_handoff_portal_ui/assets/Rietvlei.jpeg";

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
      window.requestAnimationFrame(() => {
        navigate(redirectTo, { replace: true });
      });
    } catch (err) {
      setError(
        err instanceof ApiError && err.status === 401
          ? "This Google account is not registered, or has been disabled. Contact your administrator."
          : "Sign-in failed. Please try again.",
      );
    }
  }

  return (
    <section className="auth-page">
      <div className="auth-hero" style={{ backgroundImage: `linear-gradient(90deg, rgba(15, 25, 20, 0.42), rgba(15, 25, 20, 0.2)), url(${heroImage})` }} />
      <div className="auth-panel">
        <div className="auth-card">
          <div className="auth-logo">R</div>
          <h2>Rietvlei Body Corp</h2>
          <p className="text-muted">Sign in to manage properties and users.</p>
          <div className="auth-google">
            <GoogleLogin
              onSuccess={handleSuccess}
              onError={() => setError("Google sign-in failed. Please try again.")}
              size="large"
              theme="outline"
              text="signin_with"
              shape="rectangular"
            />
          </div>
          {error && (
            <p className="error-banner" role="alert">
              {error}
            </p>
          )}
          <p className="auth-footer">Body corporate staff only</p>
        </div>
      </div>
    </section>
  );
}
