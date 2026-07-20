import { useEffect, useMemo, useState } from "react";
import { useAuth } from "../auth/AuthContext";
import { useProperties } from "../properties/useProperties";
import { useUsers } from "../users/useUsers";
import { useAssignTrustee, useCreateJob, useJobs, useUpdateJobStatus } from "./useJobs";
import type { JobDto, JobStatus } from "../../api/jobs";

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

type SortKey = "title" | "propertyName" | "status" | "createdAtUtc" | "updatedAtUtc" | "assignedTrusteeName";
type SortDirection = "asc" | "desc";

const columns: { key: SortKey; label: string }[] = [
  { key: "title", label: "Title" },
  { key: "propertyName", label: "Property" },
  { key: "status", label: "Status" },
  { key: "assignedTrusteeName", label: "Assigned to" },
  { key: "createdAtUtc", label: "Created" },
  { key: "updatedAtUtc", label: "Last updated" },
];

function sortJobs(jobs: JobDto[], sortKey: SortKey, sortDirection: SortDirection): JobDto[] {
  const factor = sortDirection === "asc" ? 1 : -1;

  return [...jobs].sort((a, b) => {
    if (sortKey === "createdAtUtc" || sortKey === "updatedAtUtc") {
      return (new Date(a[sortKey]).getTime() - new Date(b[sortKey]).getTime()) * factor;
    }

    return (a[sortKey] ?? "").localeCompare(b[sortKey] ?? "") * factor;
  });
}

export function JobsListPage() {
  const { user } = useAuth();
  const isAdmin = user?.role === "Administrator";
  const { data: jobs, isLoading, error } = useJobs();
  const { data: properties } = useProperties();
  const { data: users } = useUsers(isAdmin);
  const trustees = users?.filter((u) => u.role === "Trustee" && u.isEnabled) ?? [];
  const createJob = useCreateJob();
  const updateJobStatus = useUpdateJobStatus();
  const assignTrustee = useAssignTrustee();
  const [isAdding, setIsAdding] = useState(false);
  const [form, setForm] = useState(emptyForm);
  const [sortKey, setSortKey] = useState<SortKey>("createdAtUtc");
  const [sortDirection, setSortDirection] = useState<SortDirection>("desc");

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

  function toggleSort(key: SortKey) {
    if (key === sortKey) {
      setSortDirection((current) => (current === "asc" ? "desc" : "asc"));
    } else {
      setSortKey(key);
      setSortDirection("asc");
    }
  }

  const activeJobs = useMemo(
    () => sortJobs(jobs?.filter((job) => job.status !== "Completed") ?? [], sortKey, sortDirection),
    [jobs, sortKey, sortDirection],
  );
  const completedJobs = useMemo(
    () => sortJobs(jobs?.filter((job) => job.status === "Completed") ?? [], sortKey, sortDirection),
    [jobs, sortKey, sortDirection],
  );

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await createJob.mutateAsync(form);
    setForm(emptyForm);
    setIsAdding(false);
  }

  function renderHeaderRow() {
    return (
      <tr>
        {columns.map((column) => {
          const isActive = column.key === sortKey;
          const ariaSort = isActive ? (sortDirection === "asc" ? "ascending" : "descending") : "none";
          const arrow = isActive ? (sortDirection === "asc" ? "▲" : "▼") : "⇅";

          return (
            <th key={column.key} aria-sort={ariaSort}>
              <button
                type="button"
                className={`table-sort-button ${isActive ? "table-sort-button--active" : ""}`}
                onClick={() => toggleSort(column.key)}
              >
                {column.label}
                <span className="table-sort-button__arrow" aria-hidden="true">
                  {arrow}
                </span>
              </button>
            </th>
          );
        })}
        <th></th>
      </tr>
    );
  }

  function renderJobRow(job: JobDto) {
    return (
      <tr key={job.id}>
        <td>{job.title}</td>
        <td>{job.propertyName}</td>
        <td>
          <span className={`status-chip ${statusChipClass[job.status]}`}>{statusLabels[job.status]}</span>
        </td>
        <td>
          {isAdmin ? (
            <select
              value={job.assignedTrusteeUserId ?? ""}
              onChange={(event) =>
                assignTrustee.mutate({ id: job.id, trusteeUserId: event.target.value || null })
              }
            >
              <option value="">Unassigned</option>
              {trustees.map((trustee) => (
                <option key={trustee.id} value={trustee.id}>
                  {trustee.displayName}
                </option>
              ))}
            </select>
          ) : (
            (job.assignedTrusteeName ?? "Unassigned")
          )}
        </td>
        <td>{new Date(job.createdAtUtc).toLocaleString()}</td>
        <td>{new Date(job.updatedAtUtc).toLocaleString()}</td>
        <td>
          <div className="table-actions">
            <select
              value={job.status}
              onChange={(event) => updateJobStatus.mutate({ id: job.id, status: event.target.value as JobStatus })}
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
    );
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
        <>
          <h3 className="table-section-title">Active</h3>
          <div className="table-card">
            <table>
              <thead>{renderHeaderRow()}</thead>
              <tbody>
                {activeJobs.length === 0 ? (
                  <tr>
                    <td colSpan={columns.length + 1} className="table-empty-cell">
                      No active jobs.
                    </td>
                  </tr>
                ) : (
                  activeJobs.map(renderJobRow)
                )}
              </tbody>
            </table>
          </div>

          <h3 className="table-section-title">Completed</h3>
          <div className="table-card">
            <table>
              <thead>{renderHeaderRow()}</thead>
              <tbody>
                {completedJobs.length === 0 ? (
                  <tr>
                    <td colSpan={columns.length + 1} className="table-empty-cell">
                      No completed jobs yet.
                    </td>
                  </tr>
                ) : (
                  completedJobs.map(renderJobRow)
                )}
              </tbody>
            </table>
          </div>
        </>
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
