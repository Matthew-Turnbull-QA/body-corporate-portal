import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as jobsApi from "../../api/jobs";
import type { CreateJobRequest, JobStatus } from "../../api/jobs";

const jobsQueryKey = ["jobs"] as const;
const jobStatusHistoryQueryKey = (id: string) => ["jobs", id, "status-history"] as const;

export function useJobs() {
  return useQuery({ queryKey: jobsQueryKey, queryFn: jobsApi.listJobs });
}

export function useCreateJob() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateJobRequest) => jobsApi.createJob(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: jobsQueryKey }),
  });
}

export function useUpdateJobStatus() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, status, note }: { id: string; status: JobStatus; note: string }) =>
      jobsApi.updateJobStatus(id, status, note),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: jobsQueryKey });
      queryClient.invalidateQueries({ queryKey: jobStatusHistoryQueryKey(variables.id) });
    },
  });
}

export function useAssignTrustee() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, trusteeUserId }: { id: string; trusteeUserId: string | null }) =>
      jobsApi.assignTrustee(id, trusteeUserId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: jobsQueryKey }),
  });
}

export function useJobStatusHistory(id: string, enabled: boolean) {
  return useQuery({
    queryKey: jobStatusHistoryQueryKey(id),
    queryFn: () => jobsApi.listJobStatusHistory(id),
    enabled,
  });
}

export function useUpdateJobStatusHistoryNote() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, historyId, note }: { id: string; historyId: string; note: string }) =>
      jobsApi.updateJobStatusHistoryNote(id, historyId, note),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: jobStatusHistoryQueryKey(variables.id) });
    },
  });
}
