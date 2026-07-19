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
  signInWithGoogle: (googleIdToken: string) => Promise<void>;
  signOut: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserDto | null>(null);
  const [isSigningIn, setIsSigningIn] = useState(false);

  const signOut = useCallback(() => {
    setAccessToken(null);
    setUser(null);
  }, []);

  useEffect(() => {
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
