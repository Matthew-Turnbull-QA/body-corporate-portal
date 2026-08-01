import { apiFetch } from "./client";

export type EmailIntakeStatus = "Created" | "Duplicate" | "Failed";

export interface EmailIntakeMessageDto {
  id: string;
  providerMessageKey: string;
  messageId: string | null;
  senderEmail: string;
  senderDisplayName: string | null;
  subject: string;
  receivedAtUtc: string;
  processedAtUtc: string;
  status: EmailIntakeStatus;
  jobId: string | null;
  failureReason: string | null;
}

export interface EmailIntakePollResult {
  fetched: number;
  created: number;
  duplicatesSkipped: number;
  failed: number;
}

export function listEmailIntakeMessages() {
  return apiFetch<EmailIntakeMessageDto[]>("/api/email-intake/messages");
}

export function pollEmailIntakeNow() {
  return apiFetch<EmailIntakePollResult>("/api/email-intake/poll-now", { method: "POST" });
}
