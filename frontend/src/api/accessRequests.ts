import type { UserPermission, UserRole } from "../features/auth/types";
import { apiFetch } from "./client";

export type AccessRequestRelationship = "Trustee" | "Owner" | "Resident" | "ManagingAgent" | "Contractor" | "Other";
export type AccessRequestStatus = "Pending" | "Approved" | "Rejected";

export interface AccessRequestDto {
  id: string;
  email: string;
  displayName: string;
  phoneNumber: string;
  propertyOrUnit: string;
  relationship: AccessRequestRelationship;
  message: string;
  status: AccessRequestStatus;
  createdAtUtc: string;
  reviewedAtUtc: string | null;
  reviewedByUserId: string | null;
  existingUserId: string | null;
  existingUserIsEnabled: boolean | null;
  approvedUserId: string | null;
  reviewNote: string | null;
}

export interface SubmitAccessRequest {
  email: string;
  displayName: string;
  phoneNumber: string;
  propertyOrUnit: string;
  relationship: AccessRequestRelationship;
  message: string;
}

export interface ApproveAccessRequest {
  role: UserRole;
  permissions: UserPermission[];
  password?: string | null;
  reviewNote?: string | null;
}

export function submitAccessRequest(request: SubmitAccessRequest) {
  return apiFetch<AccessRequestDto>("/api/access-requests", { method: "POST", body: JSON.stringify(request) });
}

export function listAccessRequests() {
  return apiFetch<AccessRequestDto[]>("/api/access-requests");
}

export function approveAccessRequest(id: string, request: ApproveAccessRequest) {
  return apiFetch<AccessRequestDto>(`/api/access-requests/${id}/approve`, {
    method: "PATCH",
    body: JSON.stringify(request),
  });
}

export function rejectAccessRequest(id: string, reviewNote?: string | null) {
  return apiFetch<AccessRequestDto>(`/api/access-requests/${id}/reject`, {
    method: "PATCH",
    body: JSON.stringify({ reviewNote: reviewNote || null }),
  });
}
