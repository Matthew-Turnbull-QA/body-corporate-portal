import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as jobsApi from "../../api/jobs";
import type { CreateJobRequest, JobStatus } from "../../api/jobs";

const jobsQueryKey = ["jobs"] as const;

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
    mutationFn: ({ id, status }: { id: string; status: JobStatus }) => jobsApi.updateJobStatus(id, status),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: jobsQueryKey }),
  });
}
