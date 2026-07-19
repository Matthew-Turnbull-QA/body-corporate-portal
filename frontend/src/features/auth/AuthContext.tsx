import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from "react";
import { apiFetch, setAccessToken, setUnauthorizedHandler } from "../../api/client";
import type { UserDto } from "./types";

interface GoogleSignInResponse {
  accessToken: string;
  user: UserDto;
}

interface AuthContextValue {
  user: UserDto | null;
  isSigningIn: boolean;
  signInWithGoogle: (googleIdToken: string) => Promise<UserDto>;
  signOut: () => void;
}

const AUTH_STORAGE_KEY = "bcmp.auth";
const defaultAuthContextValue: AuthContextValue = {
  user: null,
  isSigningIn: false,
  signInWithGoogle: async () => {
    throw new Error("useAuth must be used within an AuthProvider");
  },
  signOut: () => undefined,
};
const AuthContext = createContext<AuthContextValue>(defaultAuthContextValue);

function persistAuthState(accessToken: string, user: UserDto) {
  if (typeof window === "undefined") {
    return;
  }

  window.localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify({ accessToken, user }));
}

function clearStoredAuthState() {
  if (typeof window === "undefined") {
    return;
  }

  window.localStorage.removeItem(AUTH_STORAGE_KEY);
}

function readStoredAuthState(): { accessToken: string; user: UserDto } | null {
  if (typeof window === "undefined") {
    return null;
  }

  const raw = window.localStorage.getItem(AUTH_STORAGE_KEY);
  if (!raw) {
    return null;
  }

  try {
    const parsed = JSON.parse(raw) as { accessToken?: string; user?: UserDto };
    if (!parsed.accessToken || !parsed.user) {
      return null;
    }

    return { accessToken: parsed.accessToken, user: parsed.user };
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserDto | null>(null);
  const [isSigningIn, setIsSigningIn] = useState(false);

  const signOut = useCallback(() => {
    setAccessToken(null);
    clearStoredAuthState();
    setUser(null);
  }, []);

  useEffect(() => {
    const storedAuth = readStoredAuthState();
    if (storedAuth) {
      setAccessToken(storedAuth.accessToken);
      setUser(storedAuth.user);
    }

    setUnauthorizedHandler(signOut);
    return () => setUnauthorizedHandler(null);
  }, [signOut]);

  const signInWithGoogle = useCallback(async (googleIdToken: string) => {
    setIsSigningIn(true);
    try {
      const result = await apiFetch<GoogleSignInResponse>("/api/auth/google", {
        method: "POST",
        body: JSON.stringify({ idToken: googleIdToken }),
      });
      setAccessToken(result.accessToken);
      setUser(result.user);
      persistAuthState(result.accessToken, result.user);
      return result.user;
    } finally {
      setIsSigningIn(false);
    }
  }, []);

  return (
    <AuthContext.Provider value={{ user, isSigningIn, signInWithGoogle, signOut }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
