import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as emailIntakeApi from "../../api/emailIntake";

const emailIntakeMessagesQueryKey = ["email-intake", "messages"] as const;

export function useEmailIntakeMessages() {
  return useQuery({
    queryKey: emailIntakeMessagesQueryKey,
    queryFn: emailIntakeApi.listEmailIntakeMessages,
  });
}

export function usePollEmailIntakeNow() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: emailIntakeApi.pollEmailIntakeNow,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: emailIntakeMessagesQueryKey }),
  });
}
