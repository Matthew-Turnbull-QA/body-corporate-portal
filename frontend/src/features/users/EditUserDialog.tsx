import { useState, type FormEvent } from "react";
import type { UserDto, UserRole } from "../auth/types";

interface EditUserDialogProps {
  user: UserDto;
  onSubmit: (values: { displayName: string; role: UserRole }) => Promise<void>;
  onClose: () => void;
}

export function EditUserDialog({ user, onSubmit, onClose }: EditUserDialogProps) {
  const [displayName, setDisplayName] = useState(user.displayName);
  const [role, setRole] = useState<UserRole>(user.role);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await onSubmit({ displayName, role });
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update user.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="dialog-overlay" role="dialog" aria-modal="true">
      <form className="dialog" onSubmit={handleSubmit}>
        <h3>Edit {user.email}</h3>
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
            {isSubmitting ? "Saving…" : "Save"}
          </button>
        </div>
      </form>
    </div>
  );
}
