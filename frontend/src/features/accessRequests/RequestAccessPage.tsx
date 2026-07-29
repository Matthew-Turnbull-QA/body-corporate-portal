import { useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { ApiError } from "../../api/client";
import type { AccessRequestRelationship, SubmitAccessRequest } from "../../api/accessRequests";
import { useSubmitAccessRequest } from "./useAccessRequests";
import heroImage from "../../../../docs/design_handoff_portal_ui/assets/Rietvlei.jpeg";

const relationshipOptions: { value: AccessRequestRelationship; label: string }[] = [
  { value: "Trustee", label: "Trustee" },
  { value: "Owner", label: "Owner" },
  { value: "Resident", label: "Resident" },
  { value: "ManagingAgent", label: "Managing agent" },
  { value: "Contractor", label: "Contractor" },
  { value: "Other", label: "Other" },
];

const emptyForm: SubmitAccessRequest = {
  email: "",
  displayName: "",
  phoneNumber: "",
  propertyOrUnit: "",
  relationship: "Owner",
  message: "",
};

export function RequestAccessPage() {
  const submitAccessRequest = useSubmitAccessRequest();
  const [form, setForm] = useState<SubmitAccessRequest>(emptyForm);
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    try {
      await submitAccessRequest.mutateAsync(form);
      setIsSubmitted(true);
      setForm(emptyForm);
    } catch (err) {
      setError(
        err instanceof ApiError && err.status === 409
          ? "An access request for this email is already pending, or an enabled user already exists."
          : "Failed to submit access request. Please try again.",
      );
    }
  }

  return (
    <section className="auth-page">
      <div className="auth-hero" style={{ backgroundImage: `url(${heroImage})` }} />
      <div className="auth-panel">
        <div className="auth-card auth-card--wide">
          <div className="auth-logo">R</div>
          <h2>Request access</h2>
          <p className="text-muted">Submit your details for administrator review.</p>

          {isSubmitted ? (
            <div className="success-panel" role="status">
              <h3>Request submitted</h3>
              <p>An administrator will review your details before access is enabled.</p>
              <Link className="button button--primary" to="/login">
                Back to sign in
              </Link>
            </div>
          ) : (
            <form className="auth-form" onSubmit={handleSubmit}>
              <label className="auth-field">
                Email
                <input
                  type="email"
                  required
                  value={form.email}
                  onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))}
                />
              </label>
              <label className="auth-field">
                Full name
                <input
                  required
                  value={form.displayName}
                  onChange={(event) => setForm((current) => ({ ...current, displayName: event.target.value }))}
                />
              </label>
              <label className="auth-field">
                Phone number
                <input
                  required
                  value={form.phoneNumber}
                  onChange={(event) => setForm((current) => ({ ...current, phoneNumber: event.target.value }))}
                />
              </label>
              <label className="auth-field">
                Property or unit
                <input
                  required
                  value={form.propertyOrUnit}
                  onChange={(event) => setForm((current) => ({ ...current, propertyOrUnit: event.target.value }))}
                />
              </label>
              <label className="auth-field">
                Relationship
                <select
                  value={form.relationship}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      relationship: event.target.value as AccessRequestRelationship,
                    }))
                  }
                >
                  {relationshipOptions.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </label>
              <label className="auth-field">
                Message
                <textarea
                  rows={4}
                  value={form.message}
                  onChange={(event) => setForm((current) => ({ ...current, message: event.target.value }))}
                />
              </label>
              {error && (
                <p className="error-banner" role="alert">
                  {error}
                </p>
              )}
              <button className="button button--primary auth-submit" type="submit" disabled={submitAccessRequest.isPending}>
                {submitAccessRequest.isPending ? "Submitting..." : "Submit request"}
              </button>
              <Link className="auth-text-link" to="/login">
                Back to sign in
              </Link>
            </form>
          )}
        </div>
      </div>
    </section>
  );
}
