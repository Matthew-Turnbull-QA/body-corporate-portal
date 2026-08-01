import {
  useEffect,
  useMemo,
  useState,
  type CSSProperties,
  type FormEvent,
  type KeyboardEvent as ReactKeyboardEvent,
  type MouseEvent as ReactMouseEvent,
} from "react";
import { useAuth } from "../auth/AuthContext";
import { useProperties } from "../properties/useProperties";
import { useAssignableTrustees } from "../users/useUsers";
import {
  useAssignTrustee,
  useCreateJob,
  useJobs,
  useJobStatusHistory,
  useUpdateJob,
  useUpdateJobStatus,
  useUpdateJobStatusHistoryNote,
} from "./useJobs";
import type { JobDto, JobStatus, JobStatusHistoryDto } from "../../api/jobs";

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

type SortKey =
  | "jobNumber"
  | "title"
  | "propertyName"
  | "status"
  | "createdAtUtc"
  | "updatedAtUtc"
  | "assignedTrusteeName";
type SortDirection = "asc" | "desc";
type TableColumnKey = SortKey | "actions";

const columns: { key: SortKey; label: string }[] = [
  { key: "jobNumber", label: "Job #" },
  { key: "title", label: "Title" },
  { key: "propertyName", label: "Property" },
  { key: "status", label: "Status" },
  { key: "assignedTrusteeName", label: "Assigned to" },
  { key: "createdAtUtc", label: "Created" },
  { key: "updatedAtUtc", label: "Last updated" },
];

interface StatusChangeDraft {
  job: JobDto;
  nextStatus: JobStatus;
  note: string;
}

interface UnitRequiredDraft {
  job: JobDto;
  mode: "details" | "status";
  propertyId: string;
  nextStatus?: JobStatus;
}

interface EditJobDraft {
  propertyId: string | null;
  title: string;
  description: string;
}

interface TableColumnStyle {
  key: TableColumnKey;
  style: CSSProperties;
}

function sortJobs(jobs: JobDto[], sortKey: SortKey, sortDirection: SortDirection): JobDto[] {
  const factor = sortDirection === "asc" ? 1 : -1;

  return [...jobs].sort((a, b) => {
    if (sortKey === "createdAtUtc" || sortKey === "updatedAtUtc") {
      return (new Date(a[sortKey]).getTime() - new Date(b[sortKey]).getTime()) * factor;
    }

    return (a[sortKey] ?? "").localeCompare(b[sortKey] ?? "") * factor;
  });
}

function isClosedStatus(status: JobStatus) {
  return status === "Completed" || status === "Cancelled";
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString();
}

function displayProperty(job: JobDto) {
  return job.propertyName ?? "Unit required";
}

function clamp(value: number, min: number, max: number) {
  return Math.min(Math.max(value, min), max);
}

function maxLength(values: string[]) {
  return values.reduce((max, value) => Math.max(max, value.length), 0);
}

function columnWidth(length: number, min: number, max: number) {
  return `${clamp(length + 4, min, max)}ch`;
}

function buildTableColumnStyles(jobs: JobDto[], trusteeNames: string[]): TableColumnStyle[] {
  const jobNumberLength = maxLength(["Job #", ...jobs.map((job) => job.jobNumber)]);
  const titleLength = maxLength(["Title", ...jobs.map((job) => job.title)]);
  const propertyLength = maxLength(["Property", ...jobs.map(displayProperty)]);
  const statusLength = maxLength(["Status", ...Object.values(statusLabels)]);
  const assignedLength = maxLength([
    "Assigned to",
    "Unassigned",
    ...trusteeNames,
    ...jobs.map((job) => job.assignedTrusteeName ?? "Unassigned"),
  ]);
  const createdLength = maxLength(["Created", ...jobs.map((job) => formatDateTime(job.createdAtUtc))]);
  const updatedLength = maxLength(["Last updated", ...jobs.map((job) => formatDateTime(job.updatedAtUtc))]);

  return [
    { key: "jobNumber", style: { width: columnWidth(jobNumberLength, 14, 18) } },
    { key: "title", style: { width: columnWidth(titleLength, 18, 36) } },
    { key: "propertyName", style: { width: columnWidth(propertyLength, 16, 30) } },
    { key: "status", style: { width: columnWidth(statusLength, 12, 16) } },
    { key: "assignedTrusteeName", style: { width: columnWidth(assignedLength, 16, 28) } },
    { key: "createdAtUtc", style: { width: columnWidth(createdLength, 22, 28) } },
    { key: "updatedAtUtc", style: { width: columnWidth(updatedLength, 22, 30) } },
    { key: "actions", style: { width: actionsWidth } },
  ];
}

const actionsWidth = "18ch";

function JobStatusHistoryPanel({
  job,
  canEditHistory,
}: {
  job: JobDto;
  canEditHistory: boolean;
}) {
  const { data: history, isLoading, error } = useJobStatusHistory(job.id, true);
  const updateNote = useUpdateJobStatusHistoryNote();
  const [editingHistoryId, setEditingHistoryId] = useState<string | null>(null);
  const [noteDraft, setNoteDraft] = useState("");

  function startEditing(entry: JobStatusHistoryDto) {
    setEditingHistoryId(entry.id);
    setNoteDraft(entry.note ?? "");
  }

  async function handleNoteSubmit(event: FormEvent<HTMLFormElement>, entry: JobStatusHistoryDto) {
    event.preventDefault();
    await updateNote.mutateAsync({ id: job.id, historyId: entry.id, note: noteDraft });
    setEditingHistoryId(null);
    setNoteDraft("");
  }

  if (isLoading) {
    return <div className="history-panel">Loading history...</div>;
  }

  if (error) {
    return (
      <div className="history-panel history-panel--error" role="alert">
        Failed to load status history.
      </div>
    );
  }

  if (!history || history.length === 0) {
    return <div className="history-panel">No status changes recorded yet.</div>;
  }

  return (
    <div className="history-panel">
      {history.map((entry) => {
        const isEditing = editingHistoryId === entry.id;

        return (
          <article className="history-entry" key={entry.id}>
            <div className="history-entry__summary">
              <div className="history-entry__statuses">
                <span className={`status-chip ${statusChipClass[entry.fromStatus]}`}>
                  {statusLabels[entry.fromStatus]}
                </span>
                <span className="history-entry__arrow" aria-hidden="true">
                  -
                </span>
                <span className={`status-chip ${statusChipClass[entry.toStatus]}`}>
                  {statusLabels[entry.toStatus]}
                </span>
              </div>
              <span className="table-subtext">
                {entry.changedByDisplayName} on {formatDateTime(entry.changedAtUtc)}
              </span>
            </div>

            {isEditing ? (
              <form className="history-entry__edit" onSubmit={(event) => handleNoteSubmit(event, entry)}>
                <textarea
                  value={noteDraft}
                  onChange={(event) => setNoteDraft(event.target.value)}
                  rows={3}
                  aria-label="Status history note"
                />
                {updateNote.isError && (
                  <span className="history-entry__error" role="alert">
                    Failed to save note.
                  </span>
                )}
                <div className="history-entry__actions">
                  <button
                    className="button button--ghost"
                    type="button"
                    onClick={() => {
                      setEditingHistoryId(null);
                      setNoteDraft("");
                    }}
                  >
                    Cancel
                  </button>
                  <button className="button button--primary" type="submit" disabled={updateNote.isPending}>
                    Save note
                  </button>
                </div>
              </form>
            ) : (
              <div className="history-entry__note">
                <span>{entry.note ?? "No note"}</span>
                {entry.noteEditedAtUtc && (
                  <span className="table-subtext">
                    Edited by {entry.noteEditedByDisplayName ?? "Unknown user"} on{" "}
                    {formatDateTime(entry.noteEditedAtUtc)}
                  </span>
                )}
              </div>
            )}

            {canEditHistory && !isEditing && (
              <button className="button button--ghost history-entry__edit-button" type="button" onClick={() => startEditing(entry)}>
                Edit note
              </button>
            )}
          </article>
        );
      })}
    </div>
  );
}

export function JobsListPage() {
  const { user } = useAuth();
  const canCreateJobs = Boolean(user);
  const canAssignJobs = user?.isPortalAdmin ?? false;
  const { data: jobs, isLoading, error } = useJobs();
  const { data: properties } = useProperties();
  const { data: trustees } = useAssignableTrustees(canAssignJobs);
  const createJob = useCreateJob();
  const updateJob = useUpdateJob();
  const updateJobStatus = useUpdateJobStatus();
  const assignTrustee = useAssignTrustee();
  const [isAdding, setIsAdding] = useState(false);
  const [form, setForm] = useState(emptyForm);
  const [sortKey, setSortKey] = useState<SortKey>("createdAtUtc");
  const [sortDirection, setSortDirection] = useState<SortDirection>("desc");
  const [statusChangeDraft, setStatusChangeDraft] = useState<StatusChangeDraft | null>(null);
  const [unitRequiredDraft, setUnitRequiredDraft] = useState<UnitRequiredDraft | null>(null);
  const [selectedJob, setSelectedJob] = useState<JobDto | null>(null);
  const [isDetailEditing, setIsDetailEditing] = useState(false);
  const [detailEditDraft, setDetailEditDraft] = useState<EditJobDraft>(emptyForm);

  useEffect(() => {
    if (!isAdding && !statusChangeDraft && !unitRequiredDraft && !selectedJob) {
      return;
    }

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key !== "Escape") {
        return;
      }

      if (unitRequiredDraft) {
        setUnitRequiredDraft(null);
      } else if (statusChangeDraft) {
        setStatusChangeDraft(null);
      } else if (selectedJob) {
        closeJobDetail();
      } else {
        setIsAdding(false);
      }
    };

    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [isAdding, statusChangeDraft, unitRequiredDraft, selectedJob]);

  function toggleSort(key: SortKey) {
    if (key === sortKey) {
      setSortDirection((current) => (current === "asc" ? "desc" : "asc"));
    } else {
      setSortKey(key);
      setSortDirection("asc");
    }
  }

  const activeJobs = useMemo(
    () => sortJobs(jobs?.filter((job) => !isClosedStatus(job.status)) ?? [], sortKey, sortDirection),
    [jobs, sortKey, sortDirection],
  );
  const closedJobs = useMemo(
    () => sortJobs(jobs?.filter((job) => isClosedStatus(job.status)) ?? [], sortKey, sortDirection),
    [jobs, sortKey, sortDirection],
  );
  const tableColumnStyles = useMemo(
    () => buildTableColumnStyles(jobs ?? [], trustees?.map((trustee) => trustee.displayName) ?? []),
    [jobs, trustees],
  );

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await createJob.mutateAsync(form);
    setForm(emptyForm);
    setIsAdding(false);
  }

  async function handleStatusSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!statusChangeDraft) {
      return;
    }

    try {
      await updateJobStatus.mutateAsync({
        id: statusChangeDraft.job.id,
        status: statusChangeDraft.nextStatus,
        note: statusChangeDraft.note,
      });
      setStatusChangeDraft(null);
    } catch {
      // Mutation state renders the error inside the modal.
    }
  }

  async function handleUnitRequiredSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!unitRequiredDraft || !unitRequiredDraft.propertyId) {
      return;
    }

    try {
      const updated = await updateJob.mutateAsync({
        id: unitRequiredDraft.job.id,
        request: {
          propertyId: unitRequiredDraft.propertyId,
          title: unitRequiredDraft.job.title,
          description: unitRequiredDraft.job.description,
        },
      });

      setUnitRequiredDraft(null);

      if (unitRequiredDraft.mode === "status" && unitRequiredDraft.nextStatus) {
        setStatusChangeDraft({ job: updated, nextStatus: unitRequiredDraft.nextStatus, note: "" });
        return;
      }

      setSelectedJob(updated);
      setIsDetailEditing(false);
      setDetailEditDraft({
        propertyId: updated.propertyId,
        title: updated.title,
        description: updated.description,
      });
    } catch {
      // Mutation state renders the error inside the modal.
    }
  }

  function openUnitRequiredModal(job: JobDto, mode: UnitRequiredDraft["mode"], nextStatus?: JobStatus) {
    setSelectedJob(null);
    setIsDetailEditing(false);
    setUnitRequiredDraft({ job, mode, nextStatus, propertyId: "" });
  }

  function openJobDetail(job: JobDto) {
    if (!job.propertyId && canMutateJob(job)) {
      openUnitRequiredModal(job, "details");
      return;
    }

    setSelectedJob(job);
    setIsDetailEditing(false);
    setDetailEditDraft({
      propertyId: job.propertyId,
      title: job.title,
      description: job.description,
    });
  }

  function isRowControl(target: EventTarget | null) {
    return target instanceof HTMLElement && Boolean(target.closest("button, select, input, textarea, a"));
  }

  function handleJobRowClick(job: JobDto, event: ReactMouseEvent<HTMLTableRowElement>) {
    if (isRowControl(event.target)) {
      return;
    }

    openJobDetail(job);
  }

  function handleJobRowKeyDown(job: JobDto, event: ReactKeyboardEvent<HTMLTableRowElement>) {
    if (event.currentTarget !== event.target || (event.key !== "Enter" && event.key !== " ")) {
      return;
    }

    event.preventDefault();
    openJobDetail(job);
  }

  function closeJobDetail() {
    setSelectedJob(null);
    setIsDetailEditing(false);
    setDetailEditDraft(emptyForm);
  }

  function startDetailEditing(job: JobDto) {
    setDetailEditDraft({
      propertyId: job.propertyId,
      title: job.title,
      description: job.description,
    });
    setIsDetailEditing(true);
  }

  async function handleDetailEditSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selectedJob) {
      return;
    }

    try {
      const updated = await updateJob.mutateAsync({
        id: selectedJob.id,
        request: {
          propertyId: detailEditDraft.propertyId,
          title: detailEditDraft.title,
          description: detailEditDraft.description,
        },
      });
      setSelectedJob(updated);
      setIsDetailEditing(false);
    } catch {
      // Mutation state renders the error inside the modal.
    }
  }

  function canMutateJob(job: JobDto) {
    return Boolean(user && (user.isPortalAdmin || job.assignedTrusteeUserId === user.id));
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

  function renderColumnGroup() {
    return (
      <colgroup>
        {tableColumnStyles.map((column) => (
          <col key={column.key} style={column.style} />
        ))}
      </colgroup>
    );
  }

  function renderJobRows(job: JobDto) {
    const canUpdateThisJob = canMutateJob(job);

    return (
      <tr
        className="jobs-table__row"
        key={job.id}
        tabIndex={0}
        onClick={(event) => handleJobRowClick(job, event)}
        onKeyDown={(event) => handleJobRowKeyDown(job, event)}
        aria-label={`Open details for ${job.title}`}
      >
          <td>{job.jobNumber}</td>
          <td>
            <button className="job-title-button" type="button" onClick={() => openJobDetail(job)}>
              {job.title}
            </button>
          </td>
          <td>
            <span className={job.propertyId ? undefined : "table-warning-text"}>{displayProperty(job)}</span>
          </td>
          <td>
            <span className={`status-chip ${statusChipClass[job.status]}`}>{statusLabels[job.status]}</span>
          </td>
          <td>
            {canAssignJobs ? (
              <select
                className="job-trustee-select"
                value={job.assignedTrusteeUserId ?? ""}
                onChange={(event) =>
                  assignTrustee.mutate({ id: job.id, trusteeUserId: event.target.value || null })
                }
              >
                <option value="">Unassigned</option>
                {trustees?.map((trustee) => (
                  <option key={trustee.id} value={trustee.id}>
                    {trustee.displayName}
                  </option>
                ))}
              </select>
            ) : (
              (job.assignedTrusteeName ?? "Unassigned")
            )}
          </td>
          <td>{formatDateTime(job.createdAtUtc)}</td>
          <td>{formatDateTime(job.updatedAtUtc)}</td>
          <td>
            <div className="table-actions">
              {canUpdateThisJob && (
                <select
                  className="job-status-select"
                  value={job.status}
                  onChange={(event) => {
                    const nextStatus = event.target.value as JobStatus;
                    if (nextStatus !== job.status) {
                      if (!job.propertyId) {
                        openUnitRequiredModal(job, "status", nextStatus);
                        return;
                      }

                      setStatusChangeDraft({ job, nextStatus, note: "" });
                    }
                  }}
                >
                  {statusOptions.map((status) => (
                    <option key={status} value={status}>
                      {statusLabels[status]}
                    </option>
                  ))}
                </select>
              )}
            </div>
          </td>
        </tr>
    );
  }

  function renderUnitRequiredModal(draft: UnitRequiredDraft) {
    const actionText =
      draft.mode === "status" && draft.nextStatus
        ? `before moving this job to ${statusLabels[draft.nextStatus].toLowerCase()}`
        : "before opening the job details";

    return (
      <div className="dialog-overlay" role="dialog" aria-modal="true" onClick={() => setUnitRequiredDraft(null)}>
        <form className="dialog dialog--large unit-required-modal" onClick={(event) => event.stopPropagation()} onSubmit={handleUnitRequiredSubmit}>
          <div className="unit-required-modal__header">
            <div>
              <p className="table-warning-text">Unit required</p>
              <h3>{draft.job.title}</h3>
              <p className="text-muted">
                Assign the correct unit {actionText}. The original email is shown below for reference.
              </p>
            </div>
          </div>

          <dl className="job-detail-grid">
            <div>
              <dt>Job #</dt>
              <dd>{draft.job.jobNumber}</dd>
            </div>
            <div>
              <dt>Status</dt>
              <dd>{statusLabels[draft.job.status]}</dd>
            </div>
            <div>
              <dt>Assigned to</dt>
              <dd>{draft.job.assignedTrusteeName ?? "Unassigned"}</dd>
            </div>
            <div>
              <dt>Created</dt>
              <dd>{formatDateTime(draft.job.createdAtUtc)}</dd>
            </div>
          </dl>

          <label className="dialog__field">
            Unit / property
            <select
              value={draft.propertyId}
              onChange={(event) =>
                setUnitRequiredDraft((current) =>
                  current ? { ...current, propertyId: event.target.value } : current,
                )
              }
              required
            >
              <option value="">Select a unit</option>
              {properties?.map((property) => (
                <option key={property.id} value={property.id}>
                  {property.name}
                </option>
              ))}
            </select>
          </label>

          <section className="job-detail-description">
            <h4>Original email</h4>
            <pre className="email-preview">{draft.job.description || "No email content."}</pre>
          </section>

          {updateJob.isError && (
            <p className="error-banner" role="alert">
              Failed to assign unit.
            </p>
          )}
          <div className="dialog__actions">
            <button className="button button--ghost" type="button" onClick={() => setUnitRequiredDraft(null)}>
              Cancel
            </button>
            <button className="button button--primary" type="submit" disabled={updateJob.isPending || !draft.propertyId}>
              Save unit
            </button>
          </div>
        </form>
      </div>
    );
  }

  function renderJobDetailModal(job: JobDto) {
    const canUpdateThisJob = canMutateJob(job);

    return (
      <div className="dialog-overlay" role="dialog" aria-modal="true" onClick={closeJobDetail}>
        <div className="dialog dialog--large job-detail-modal" onClick={(event) => event.stopPropagation()}>
          <div className="job-detail-modal__header">
            <div>
              <span className={`status-chip ${statusChipClass[job.status]}`}>{statusLabels[job.status]}</span>
              <h3>{job.title}</h3>
              <p className={job.propertyId ? "text-muted" : "table-warning-text"}>{displayProperty(job)}</p>
            </div>
            <div className="job-detail-modal__actions">
              {canUpdateThisJob && !isDetailEditing && (
                <button className="button button--ghost" type="button" onClick={() => startDetailEditing(job)}>
                  Edit
                </button>
              )}
              <button className="button button--ghost" type="button" onClick={closeJobDetail}>
                Close
              </button>
            </div>
          </div>

          {isDetailEditing ? (
            <form className="job-detail-edit" onSubmit={handleDetailEditSubmit}>
              <label className="dialog__field">
                Property
                <select
                  value={detailEditDraft.propertyId ?? ""}
                  onChange={(event) =>
                    setDetailEditDraft((current) => ({ ...current, propertyId: event.target.value || null }))
                  }
                >
                  <option value="">Unit required</option>
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
                  value={detailEditDraft.title}
                  onChange={(event) => setDetailEditDraft((current) => ({ ...current, title: event.target.value }))}
                  required
                />
              </label>
              <label className="dialog__field">
                Description
                <textarea
                  value={detailEditDraft.description}
                  onChange={(event) =>
                    setDetailEditDraft((current) => ({ ...current, description: event.target.value }))
                  }
                  rows={4}
                />
              </label>
              {updateJob.isError && (
                <p className="error-banner" role="alert">
                  Failed to update job.
                </p>
              )}
              <div className="dialog__actions">
                <button className="button button--ghost" type="button" onClick={() => setIsDetailEditing(false)}>
                  Cancel
                </button>
                <button className="button button--primary" type="submit" disabled={updateJob.isPending}>
                  Save
                </button>
              </div>
            </form>
          ) : (
            <div className="job-detail-body">
              <dl className="job-detail-grid">
                <div>
                  <dt>Job #</dt>
                  <dd>{job.jobNumber}</dd>
                </div>
                <div>
                  <dt>Assigned to</dt>
                  <dd>{job.assignedTrusteeName ?? "Unassigned"}</dd>
                </div>
                <div>
                  <dt>Created</dt>
                  <dd>{formatDateTime(job.createdAtUtc)}</dd>
                </div>
                <div>
                  <dt>Last updated</dt>
                  <dd>{formatDateTime(job.updatedAtUtc)}</dd>
                </div>
                <div>
                  <dt>Source</dt>
                  <dd>{job.source}</dd>
                </div>
              </dl>
              <section className="job-detail-description">
                <h4>Description</h4>
                <p>{job.description || "No description."}</p>
              </section>
            </div>
          )}

          <section className="job-detail-history">
            <div className="job-detail-history__header">
              <h4>Notes & history</h4>
            </div>
            <div className="job-detail-history__scroll">
              <JobStatusHistoryPanel job={job} canEditHistory={canUpdateThisJob} />
            </div>
          </section>
        </div>
      </div>
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
          disabled={!canCreateJobs || !properties || properties.length === 0}
          title={
            !canCreateJobs
              ? "Sign in before creating jobs"
              : !properties || properties.length === 0
                ? "Add a property before creating a job"
                : undefined
          }
        >
          Add job
        </button>
      </div>

      {isLoading && <div className="state-card">Loading jobs...</div>}

      {error && (
        <p className="error-banner" role="alert">
          Failed to load jobs.
        </p>
      )}

      {!isLoading && !error && (
        <>
          <h3 className="table-section-title">Active</h3>
          <div className="table-card">
            <table className="jobs-table">
              {renderColumnGroup()}
              <thead>{renderHeaderRow()}</thead>
              <tbody>
                {activeJobs.length === 0 ? (
                  <tr>
                    <td colSpan={columns.length + 1} className="table-empty-cell">
                      No active jobs.
                    </td>
                  </tr>
                ) : (
                  activeJobs.map(renderJobRows)
                )}
              </tbody>
            </table>
          </div>

          <h3 className="table-section-title">Closed</h3>
          <div className="table-card">
            <table className="jobs-table">
              {renderColumnGroup()}
              <thead>{renderHeaderRow()}</thead>
              <tbody>
                {closedJobs.length === 0 ? (
                  <tr>
                    <td colSpan={columns.length + 1} className="table-empty-cell">
                      No closed jobs yet.
                    </td>
                  </tr>
                ) : (
                  closedJobs.map(renderJobRows)
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

      {statusChangeDraft && (
        <div className="dialog-overlay" role="dialog" aria-modal="true" onClick={() => setStatusChangeDraft(null)}>
          <form className="dialog" onClick={(event) => event.stopPropagation()} onSubmit={handleStatusSubmit}>
            <h3>Update status</h3>
            <div className="status-change-summary">
              <span className={`status-chip ${statusChipClass[statusChangeDraft.job.status]}`}>
                {statusLabels[statusChangeDraft.job.status]}
              </span>
              <span className="history-entry__arrow" aria-hidden="true">
                -
              </span>
              <span className={`status-chip ${statusChipClass[statusChangeDraft.nextStatus]}`}>
                {statusLabels[statusChangeDraft.nextStatus]}
              </span>
            </div>
            <label className="dialog__field">
              Notes
              <textarea
                value={statusChangeDraft.note}
                onChange={(event) =>
                  setStatusChangeDraft((current) =>
                    current ? { ...current, note: event.target.value } : current,
                  )
                }
                rows={4}
              />
            </label>
            {updateJobStatus.isError && (
              <p className="error-banner" role="alert">
                Failed to update status.
              </p>
            )}
            <div className="dialog__actions">
              <button className="button button--ghost" type="button" onClick={() => setStatusChangeDraft(null)}>
                Cancel
              </button>
              <button className="button button--primary" type="submit" disabled={updateJobStatus.isPending}>
                Save
              </button>
            </div>
          </form>
        </div>
      )}

      {unitRequiredDraft && renderUnitRequiredModal(unitRequiredDraft)}
      {selectedJob && renderJobDetailModal(selectedJob)}
    </section>
  );
}
