import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as assignmentRulesApi from "../../api/assignmentRules";
import type { SaveAssignmentRuleRequest } from "../../api/assignmentRules";

const assignmentRulesQueryKey = ["assignment-rules"] as const;

export function useAssignmentRules() {
  return useQuery({ queryKey: assignmentRulesQueryKey, queryFn: assignmentRulesApi.listAssignmentRules });
}

export function useCreateAssignmentRule() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: SaveAssignmentRuleRequest) => assignmentRulesApi.createAssignmentRule(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: assignmentRulesQueryKey }),
  });
}

export function useUpdateAssignmentRule() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: SaveAssignmentRuleRequest }) =>
      assignmentRulesApi.updateAssignmentRule(id, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: assignmentRulesQueryKey }),
  });
}

export function useToggleAssignmentRule() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, isEnabled }: { id: string; isEnabled: boolean }) =>
      isEnabled ? assignmentRulesApi.enableAssignmentRule(id) : assignmentRulesApi.disableAssignmentRule(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: assignmentRulesQueryKey }),
  });
}

export function useReorderAssignmentRules() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (ruleIds: string[]) => assignmentRulesApi.reorderAssignmentRules(ruleIds),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: assignmentRulesQueryKey }),
  });
}
