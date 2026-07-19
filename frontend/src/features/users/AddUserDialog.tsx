import { useState, type FormEvent } from "react";
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
    <div className="dialog-overlay" role="dialog" aria-modal="true">
      <form className="dialog" onSubmit={handleSubmit}>
        <h3>Add user</h3>
        <label>
          Email
          <input type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
        </label>
        <label>
          Display name
          <input required value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
        </label>
        <label>
          Role
          <select value={role} onChange={(e) => setRole(e.target.value as UserRole)}>
            <option value="Trustee">Trustee</option>
            <option value="Administrator">Administrator</option>
          </select>
        </label>
        {error && <p role="alert">{error}</p>}
        <div className="dialog__actions">
          <button type="button" onClick={onClose} disabled={isSubmitting}>
            Cancel
          </button>
          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Adding…" : "Add"}
          </button>
        </div>
      </form>
    </div>
  );
}
