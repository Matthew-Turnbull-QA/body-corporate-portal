import { useEffect, useState, type FormEvent } from "react";
import type { UserDto } from "../auth/types";

interface EditUserDialogProps {
  user: UserDto;
  onSubmit: (values: {
    displayName: string;
    isPortalAdmin: boolean;
    newPassword: string | null;
  }) => Promise<void>;
  onClose: () => void;
}

export function EditUserDialog({ user, onSubmit, onClose }: EditUserDialogProps) {
  const [displayName, setDisplayName] = useState(user.displayName);
  const [isPortalAdmin, setIsPortalAdmin] = useState(user.isPortalAdmin);
  const [newPassword, setNewPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose();
      }
    };

    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [onClose]);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await onSubmit({ displayName, isPortalAdmin, newPassword: newPassword.trim() || null });
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update user.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="dialog-overlay" role="dialog" aria-modal="true" onClick={onClose}>
      <form className="dialog" onClick={(event) => event.stopPropagation()} onSubmit={handleSubmit}>
        <h3>Edit {user.email}</h3>
        <label className="dialog__field">
          Display name
          <input required value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
        </label>
        <label className="checkbox-option">
          <input
            type="checkbox"
            checked={isPortalAdmin}
            onChange={(event) => setIsPortalAdmin(event.target.checked)}
          />
          Portal admin
        </label>
        <label className="dialog__field">
          New local password
          <input
            type="password"
            minLength={8}
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
          />
        </label>
        {error && (
          <p className="error-banner" role="alert">
            {error}
          </p>
        )}
        <div className="dialog__actions">
          <button className="button button--ghost" type="button" onClick={onClose} disabled={isSubmitting}>
            Cancel
          </button>
          <button className="button button--primary" type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Saving..." : "Save"}
          </button>
        </div>
      </form>
    </div>
  );
}
