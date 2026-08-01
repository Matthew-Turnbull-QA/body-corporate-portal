import { apiFetch } from "./client";

export type AssignmentNotificationType = "Assigned" | "ReassignedTo" | "ReassignedAway" | "RoutingWarning";

export interface AssignmentNotificationDto {
  id: string;
  recipientUserId: string;
  jobId: string | null;
  jobNumber: string | null;
  type: AssignmentNotificationType;
  subject: string;
  message: string;
  createdAtUtc: string;
  emailSentAtUtc: string | null;
  emailFailureReason: string | null;
}

export function listNotifications() {
  return apiFetch<AssignmentNotificationDto[]>("/api/notifications");
}
