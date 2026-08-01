import { apiFetch } from "./client";
import type { JobSource } from "./jobs";

export interface AssignmentRuleDto {
  id: string;
  name: string;
  isEnabled: boolean;
  priority: number;
  targetTrusteeUserId: string;
  targetTrusteeName: string;
  propertyId: string | null;
  propertyName: string | null;
  jobSource: JobSource | null;
  keywords: string[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface SaveAssignmentRuleRequest {
  name: string;
  targetTrusteeUserId: string;
  propertyId: string | null;
  jobSource: JobSource | null;
  keywords: string[];
  isEnabled?: boolean;
}

export function listAssignmentRules() {
  return apiFetch<AssignmentRuleDto[]>("/api/assignment-rules");
}

export function createAssignmentRule(request: SaveAssignmentRuleRequest) {
  return apiFetch<AssignmentRuleDto>("/api/assignment-rules", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function updateAssignmentRule(id: string, request: SaveAssignmentRuleRequest) {
  return apiFetch<AssignmentRuleDto>(`/api/assignment-rules/${id}`, {
    method: "PUT",
    body: JSON.stringify(request),
  });
}

export function enableAssignmentRule(id: string) {
  return apiFetch<AssignmentRuleDto>(`/api/assignment-rules/${id}/enable`, { method: "PATCH" });
}

export function disableAssignmentRule(id: string) {
  return apiFetch<AssignmentRuleDto>(`/api/assignment-rules/${id}/disable`, { method: "PATCH" });
}

export function reorderAssignmentRules(ruleIds: string[]) {
  return apiFetch<AssignmentRuleDto[]>("/api/assignment-rules/order", {
    method: "PUT",
    body: JSON.stringify({ ruleIds }),
  });
}
