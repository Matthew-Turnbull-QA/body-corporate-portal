import { apiFetch } from "./client";

export type JobStatus = "Open" | "InProgress" | "Completed" | "Cancelled";
export type JobSource = "Manual" | "Email";

export interface JobDto {
  id: string;
  propertyId: string;
  propertyName: string;
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

export interface CreateJobRequest {
  propertyId: string;
  title: string;
  description: string;
}

export function listJobs() {
  return apiFetch<JobDto[]>("/api/jobs");
}

export function createJob(request: CreateJobRequest) {
  return apiFetch<JobDto>("/api/jobs", { method: "POST", body: JSON.stringify(request) });
}

export function updateJobStatus(id: string, status: JobStatus) {
  return apiFetch<JobDto>(`/api/jobs/${id}/status`, {
    method: "PATCH",
    body: JSON.stringify({ status }),
  });
}

export function assignTrustee(id: string, trusteeUserId: string | null) {
  return apiFetch<JobDto>(`/api/jobs/${id}/assign`, {
    method: "PATCH",
    body: JSON.stringify({ trusteeUserId }),
  });
}
