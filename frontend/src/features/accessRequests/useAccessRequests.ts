import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as accessRequestsApi from "../../api/accessRequests";
import type { ApproveAccessRequest, SubmitAccessRequest } from "../../api/accessRequests";

const accessRequestsQueryKey = ["access-requests"] as const;

export function useAccessRequests() {
  return useQuery({ queryKey: accessRequestsQueryKey, queryFn: accessRequestsApi.listAccessRequests });
}

export function useSubmitAccessRequest() {
  return useMutation({
    mutationFn: (request: SubmitAccessRequest) => accessRequestsApi.submitAccessRequest(request),
  });
}

export function useApproveAccessRequest() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: ApproveAccessRequest }) =>
      accessRequestsApi.approveAccessRequest(id, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: accessRequestsQueryKey }),
  });
}

export function useRejectAccessRequest() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, reviewNote }: { id: string; reviewNote?: string | null }) =>
      accessRequestsApi.rejectAccessRequest(id, reviewNote),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: accessRequestsQueryKey }),
  });
}
