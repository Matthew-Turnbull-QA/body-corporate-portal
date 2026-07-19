import { useEffect, useState } from "react";
import { useProperties, useCreateProperty, useUpdateProperty } from "./useProperties";
import { EditPropertyDialog } from "./EditPropertyDialog";

const emptyForm = {
  name: "",
  addressLine1: "",
  suburb: "",
  state: "",
  postcode: "",
};

export function PropertiesListPage() {
  const { data: properties, isLoading, error } = useProperties();
  const createProperty = useCreateProperty();
  const updateProperty = useUpdateProperty();
  const [isAdding, setIsAdding] = useState(false);
  const [editingProperty, setEditingProperty] = useState<string | null>(null);
  const [form, setForm] = useState(emptyForm);

  useEffect(() => {
    if (!isAdding) {
      return;
    }

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setIsAdding(false);
      }
    };

    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [isAdding]);

  const propertyToEdit = properties?.find((property) => property.id === editingProperty) ?? null;

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await createProperty.mutateAsync(form);
    setForm(emptyForm);
    setIsAdding(false);
  }

  async function handleUpdate(values: { name: string; addressLine1: string; suburb: string; state: string; postcode: string }) {
    if (!propertyToEdit) return;
    await updateProperty.mutateAsync({ id: propertyToEdit.id, request: values });
    setEditingProperty(null);
  }

  return (
    <section className="page-card">
      <div className="page-header">
        <h2 className="page-title">Properties</h2>
        <button className="button button--primary" type="button" onClick={() => setIsAdding(true)}>
          Add property
        </button>
      </div>

      {isLoading && <div className="state-card">Loading properties…</div>}

      {error && (
        <p className="error-banner" role="alert">
          Failed to load properties.
        </p>
      )}

      {!isLoading && !error && (
        <div className="table-card">
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Address</th>
                <th>Suburb</th>
                <th>State</th>
                <th>Postcode</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {properties?.map((property) => (
                <tr key={property.id}>
                  <td>{property.name}</td>
                  <td>{property.addressLine1}</td>
                  <td>{property.suburb}</td>
                  <td>{property.state}</td>
                  <td>{property.postcode}</td>
                  <td>
                    <div className="table-actions">
                      <button className="button button--ghost" type="button" onClick={() => setEditingProperty(property.id)}>
                        Edit
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {isAdding && (
        <div className="dialog-overlay" role="dialog" aria-modal="true" onClick={() => setIsAdding(false)}>
          <form className="dialog" onClick={(event) => event.stopPropagation()} onSubmit={handleSubmit}>
            <h3>Add property</h3>
            <label className="dialog__field">
              Name
              <input
                value={form.name}
                onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
                required
              />
            </label>
            <label className="dialog__field">
              Address
              <input
                value={form.addressLine1}
                onChange={(event) => setForm((current) => ({ ...current, addressLine1: event.target.value }))}
                required
              />
            </label>
            <label className="dialog__field">
              Suburb
              <input
                value={form.suburb}
                onChange={(event) => setForm((current) => ({ ...current, suburb: event.target.value }))}
                required
              />
            </label>
            <div className="dialog__grid">
              <label className="dialog__field">
                State
                <input
                  value={form.state}
                  onChange={(event) => setForm((current) => ({ ...current, state: event.target.value }))}
                  required
                />
              </label>
              <label className="dialog__field">
                Postcode
                <input
                  value={form.postcode}
                  onChange={(event) => setForm((current) => ({ ...current, postcode: event.target.value }))}
                  required
                />
              </label>
            </div>
            <div className="dialog__actions">
              <button className="button button--ghost" type="button" onClick={() => setIsAdding(false)}>
                Cancel
              </button>
              <button className="button button--primary" type="submit">
                Save
              </button>
            </div>
          </form>
        </div>
      )}

      {propertyToEdit && (
        <EditPropertyDialog
          property={propertyToEdit}
          onSubmit={handleUpdate}
          onClose={() => setEditingProperty(null)}
        />
      )}
    </section>
  );
}
