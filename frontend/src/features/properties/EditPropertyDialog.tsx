import { useState, type FormEvent } from "react";
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
    <div className="dialog-overlay" role="dialog" aria-modal="true">
      <form className="dialog" onSubmit={handleSubmit}>
        <h3>Edit property</h3>
        <label>
          Name
          <input
            required
            value={form.name}
            onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
          />
        </label>
        <label>
          Address line 1
          <input
            required
            value={form.addressLine1}
            onChange={(event) => setForm((current) => ({ ...current, addressLine1: event.target.value }))}
          />
        </label>
        <label>
          Suburb
          <input
            required
            value={form.suburb}
            onChange={(event) => setForm((current) => ({ ...current, suburb: event.target.value }))}
          />
        </label>
        <label>
          State
          <input
            required
            value={form.state}
            onChange={(event) => setForm((current) => ({ ...current, state: event.target.value }))}
          />
        </label>
        <label>
          Postcode
          <input
            required
            value={form.postcode}
            onChange={(event) => setForm((current) => ({ ...current, postcode: event.target.value }))}
          />
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
