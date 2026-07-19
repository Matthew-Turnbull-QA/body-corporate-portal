import { useEffect, useState, type FormEvent } from "react";
import type { CreatePropertyRequest, PropertyDto } from "../../api/properties";

interface EditPropertyDialogProps {
  property: PropertyDto;
  onSubmit: (values: CreatePropertyRequest) => Promise<void>;
  onClose: () => void;
}

export function EditPropertyDialog({ property, onSubmit, onClose }: EditPropertyDialogProps) {
  const [form, setForm] = useState<CreatePropertyRequest>({
    name: property.name,
    addressLine1: property.addressLine1,
    suburb: property.suburb,
    state: property.state,
    postcode: property.postcode,
  });
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
      await onSubmit(form);
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save property.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="dialog-overlay" role="dialog" aria-modal="true" onClick={onClose}>
      <form className="dialog" onClick={(event) => event.stopPropagation()} onSubmit={handleSubmit}>
        <h3>Edit property</h3>
        <label className="dialog__field">
          Name
          <input
            required
            value={form.name}
            onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
          />
        </label>
        <label className="dialog__field">
          Address
          <input
            required
            value={form.addressLine1}
            onChange={(event) => setForm((current) => ({ ...current, addressLine1: event.target.value }))}
          />
        </label>
        <label className="dialog__field">
          Suburb
          <input
            required
            value={form.suburb}
            onChange={(event) => setForm((current) => ({ ...current, suburb: event.target.value }))}
          />
        </label>
        <div className="dialog__grid">
          <label className="dialog__field">
            State
            <input
              required
              value={form.state}
              onChange={(event) => setForm((current) => ({ ...current, state: event.target.value }))}
            />
          </label>
          <label className="dialog__field">
            Postcode
            <input
              required
              value={form.postcode}
              onChange={(event) => setForm((current) => ({ ...current, postcode: event.target.value }))}
            />
          </label>
        </div>
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
            {isSubmitting ? "Saving…" : "Save"}
          </button>
        </div>
      </form>
    </div>
  );
}
