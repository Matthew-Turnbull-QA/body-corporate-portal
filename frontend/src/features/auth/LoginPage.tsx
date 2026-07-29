import { GoogleLogin, type CredentialResponse } from "@react-oauth/google";
import { useEffect, useState, type FormEvent } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { ApiError } from "../../api/client";
import { useAuth } from "./AuthContext";
import heroImage from "../../../../docs/design_handoff_portal_ui/assets/Rietvlei.jpeg";

export function LoginPage() {
  const { user, isSigningIn, signInWithGoogle, signInWithPassword } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);

  const redirectTo = (location.state as { from?: Location } | null)?.from?.pathname ?? "/";

  useEffect(() => {
    if (user) {
      navigate(redirectTo, { replace: true });
    }
  }, [navigate, redirectTo, user]);

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

  async function handlePasswordSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    try {
      await signInWithPassword(email, password);
      window.requestAnimationFrame(() => {
        navigate(redirectTo, { replace: true });
      });
    } catch (err) {
      setError(
        err instanceof ApiError && err.status === 401
          ? "The email or password is incorrect, the user has no local password, or the account has been disabled."
          : "Sign-in failed. Please try again.",
      );
    }
  }

  if (user) {
    return null;
  }

  return (
    <section className="auth-page">
      <div className="auth-hero" style={{ backgroundImage: `url(${heroImage})` }} />
      <div className="auth-panel">
        <div className="auth-card">
          <div className="auth-logo">R</div>
          <h2>Rietvlei Body Corp</h2>
          <p className="text-muted">Sign in to manage properties and users.</p>
          <form className="auth-form" onSubmit={handlePasswordSubmit}>
            <label className="auth-field">
              Email
              <input type="email" required value={email} onChange={(event) => setEmail(event.target.value)} />
            </label>
            <label className="auth-field">
              Password
              <input
                type="password"
                required
                value={password}
                onChange={(event) => setPassword(event.target.value)}
              />
            </label>
            <button className="button button--primary auth-submit" type="submit" disabled={isSigningIn}>
              {isSigningIn ? "Signing in..." : "Sign in"}
            </button>
          </form>
          <div className="auth-divider">
            <span>or</span>
          </div>
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
          <p className="auth-footer">
            Don&apos;t have an account? <Link className="auth-text-link" to="/request-access">Request access</Link>
          </p>
          <p className="auth-footer">Body corporate staff only</p>
        </div>
      </div>
    </section>
  );
}
