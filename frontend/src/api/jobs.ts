import { apiFetch } from "./client";

export type JobStatus = "Open" | "InProgress" | "Completed" | "Cancelled";
export type JobSource = "Manual" | "Email";

export interface JobDto {
  id: string;
  jobNumber: string;
  propertyId: string | null;
  propertyName: string | null;
  title: string;
  description: string;
  status: JobStatus;
  source: JobSource;
  createdByUserId: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  assignedTrusteeUserId: string | null;
  assignedTrusteeName: string | null;
}

export interface JobStatusHistoryDto {
  id: string;
  jobId: string;
  fromStatus: JobStatus;
  toStatus: JobStatus;
  note: string | null;
  changedByUserId: string;
  changedByDisplayName: string;
  changedAtUtc: string;
  noteEditedByUserId: string | null;
  noteEditedByDisplayName: string | null;
  noteEditedAtUtc: string | null;
}

export interface CreateJobRequest {
  propertyId: string;
  title: string;
  description: string;
}

export interface UpdateJobRequest {
  propertyId: string | null;
  title: string;
  description: string;
}

export function listJobs() {
  return apiFetch<JobDto[]>("/api/jobs");
}

export function createJob(request: CreateJobRequest) {
  return apiFetch<JobDto>("/api/jobs", { method: "POST", body: JSON.stringify(request) });
}

export function updateJob(id: string, request: UpdateJobRequest) {
  return apiFetch<JobDto>(`/api/jobs/${id}`, { method: "PUT", body: JSON.stringify(request) });
}

export function updateJobStatus(id: string, status: JobStatus, note: string) {
  return apiFetch<JobDto>(`/api/jobs/${id}/status`, {
    method: "PATCH",
    body: JSON.stringify({ status, note }),
  });
}

export function assignTrustee(id: string, trusteeUserId: string | null) {
  return apiFetch<JobDto>(`/api/jobs/${id}/assign`, {
    method: "PATCH",
    body: JSON.stringify({ trusteeUserId }),
  });
}

export function listJobStatusHistory(id: string) {
  return apiFetch<JobStatusHistoryDto[]>(`/api/jobs/${id}/status-history`);
}

export function updateJobStatusHistoryNote(id: string, historyId: string, note: string) {
  return apiFetch<JobStatusHistoryDto>(`/api/jobs/${id}/status-history/${historyId}/note`, {
    method: "PATCH",
    body: JSON.stringify({ note }),
  });
}
