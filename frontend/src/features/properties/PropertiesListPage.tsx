import { useState } from "react";
import { useProperties, useCreateProperty } from "./useProperties";

export function PropertiesListPage() {
  const { data: properties, isLoading, error } = useProperties();
  const createProperty = useCreateProperty();
  const [isAdding, setIsAdding] = useState(false);
  const [form, setForm] = useState({
    name: "",
    addressLine1: "",
    suburb: "",
    state: "",
    postcode: "",
  });

  if (isLoading) {
    return <p>Loading properties…</p>;
  }

  if (error) {
    return <p role="alert">Failed to load properties.</p>;
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await createProperty.mutateAsync(form);
    setForm({ name: "", addressLine1: "", suburb: "", state: "", postcode: "" });
    setIsAdding(false);
  }

  return (
    <section>
      <div className="users-page__header">
        <h2>Properties</h2>
        <button type="button" onClick={() => setIsAdding(true)}>
          Add property
        </button>
      </div>

      <table>
        <thead>
          <tr>
            <th>Name</th>
            <th>Address</th>
            <th>Suburb</th>
            <th>State</th>
            <th>Postcode</th>
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
            </tr>
          ))}
        </tbody>
      </table>

      {isAdding && (
        <form onSubmit={handleSubmit} style={{ display: "grid", gap: "0.5rem", maxWidth: "24rem", marginTop: "1rem" }}>
          <input
            placeholder="Name"
            value={form.name}
            onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
            required
          />
          <input
            placeholder="Address"
            value={form.addressLine1}
            onChange={(event) => setForm((current) => ({ ...current, addressLine1: event.target.value }))}
            required
          />
          <input
            placeholder="Suburb"
            value={form.suburb}
            onChange={(event) => setForm((current) => ({ ...current, suburb: event.target.value }))}
            required
          />
          <input
            placeholder="State"
            value={form.state}
            onChange={(event) => setForm((current) => ({ ...current, state: event.target.value }))}
            required
          />
          <input
            placeholder="Postcode"
            value={form.postcode}
            onChange={(event) => setForm((current) => ({ ...current, postcode: event.target.value }))}
            required
          />
          <div style={{ display: "flex", gap: "0.5rem" }}>
            <button type="submit">Save</button>
            <button type="button" onClick={() => setIsAdding(false)}>
              Cancel
            </button>
          </div>
        </form>
      )}
    </section>
  );
}
