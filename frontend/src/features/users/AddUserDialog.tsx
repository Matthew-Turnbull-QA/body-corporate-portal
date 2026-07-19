import { useEffect, useState, type FormEvent } from "react";
import type { UserRole } from "../auth/types";

interface AddUserDialogProps {
  onSubmit: (values: { email: string; displayName: string; role: UserRole }) => Promise<void>;
  onClose: () => void;
}

export function AddUserDialog({ onSubmit, onClose }: AddUserDialogProps) {
  const [email, setEmail] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [role, setRole] = useState<UserRole>("Trustee");
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
      await onSubmit({ email, displayName, role });
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to add user.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="dialog-overlay" role="dialog" aria-modal="true" onClick={onClose}>
      <form className="dialog" onClick={(event) => event.stopPropagation()} onSubmit={handleSubmit}>
        <h3>Add user</h3>
        <label className="dialog__field">
          Email
          <input type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
        </label>
        <label className="dialog__field">
          Display name
          <input required value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
        </label>
        <label className="dialog__field">
          Role
          <select value={role} onChange={(e) => setRole(e.target.value as UserRole)}>
            <option value="Trustee">Trustee</option>
            <option value="Administrator">Administrator</option>
          </select>
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
            {isSubmitting ? "Adding…" : "Add"}
          </button>
        </div>
      </form>
    </div>
  );
}
