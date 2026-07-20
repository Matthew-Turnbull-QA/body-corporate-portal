import { useEffect, useState } from "react";
import { useProperties } from "../properties/useProperties";
import { useCreateJob, useJobs, useUpdateJobStatus } from "./useJobs";
import type { JobStatus } from "../../api/jobs";

const emptyForm = {
  propertyId: "",
  title: "",
  description: "",
};

const statusOptions: JobStatus[] = ["Open", "InProgress", "Completed", "Cancelled"];

const statusLabels: Record<JobStatus, string> = {
  Open: "Open",
  InProgress: "In progress",
  Completed: "Completed",
  Cancelled: "Cancelled",
};

const statusChipClass: Record<JobStatus, string> = {
  Open: "status-chip--open",
  InProgress: "status-chip--inprogress",
  Completed: "status-chip--completed",
  Cancelled: "status-chip--cancelled",
};

export function JobsListPage() {
  const { data: jobs, isLoading, error } = useJobs();
  const { data: properties } = useProperties();
  const createJob = useCreateJob();
  const updateJobStatus = useUpdateJobStatus();
  const [isAdding, setIsAdding] = useState(false);
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

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await createJob.mutateAsync(form);
    setForm(emptyForm);
    setIsAdding(false);
  }

  return (
    <section className="page-card">
      <div className="page-header">
        <h2 className="page-title">Jobs</h2>
        <button
          className="button button--primary"
          type="button"
          onClick={() => setIsAdding(true)}
          disabled={!properties || properties.length === 0}
          title={!properties || properties.length === 0 ? "Add a property before creating a job" : undefined}
        >
          Add job
        </button>
      </div>

      {isLoading && <div className="state-card">Loading jobs…</div>}

      {error && (
        <p className="error-banner" role="alert">
          Failed to load jobs.
        </p>
      )}

      {!isLoading && !error && (
        <div className="table-card">
          <table>
            <thead>
              <tr>
                <th>Title</th>
                <th>Property</th>
                <th>Status</th>
                <th>Created</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {jobs?.map((job) => (
                <tr key={job.id}>
                  <td>{job.title}</td>
                  <td>{job.propertyName}</td>
                  <td>
                    <span className={`status-chip ${statusChipClass[job.status]}`}>{statusLabels[job.status]}</span>
                  </td>
                  <td>{new Date(job.createdAtUtc).toLocaleString()}</td>
                  <td>
                    <div className="table-actions">
                      <select
                        value={job.status}
                        onChange={(event) =>
                          updateJobStatus.mutate({ id: job.id, status: event.target.value as JobStatus })
                        }
                      >
                        {statusOptions.map((status) => (
                          <option key={status} value={status}>
                            {statusLabels[status]}
                          </option>
                        ))}
                      </select>
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
            <h3>Add job</h3>
            <label className="dialog__field">
              Property
              <select
                value={form.propertyId}
                onChange={(event) => setForm((current) => ({ ...current, propertyId: event.target.value }))}
                required
              >
                <option value="" disabled>
                  Select a property
                </option>
                {properties?.map((property) => (
                  <option key={property.id} value={property.id}>
                    {property.name}
                  </option>
                ))}
              </select>
            </label>
            <label className="dialog__field">
              Title
              <input
                value={form.title}
                onChange={(event) => setForm((current) => ({ ...current, title: event.target.value }))}
                required
              />
            </label>
            <label className="dialog__field">
              Description
              <textarea
                value={form.description}
                onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
                rows={4}
              />
            </label>
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
    </section>
  );
}
