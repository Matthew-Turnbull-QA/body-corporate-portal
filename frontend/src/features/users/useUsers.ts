import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as usersApi from "../../api/users";
import type { CreateUserRequest, UpdateUserRequest } from "../../api/users";

const usersQueryKey = ["users"] as const;

export function useUsers() {
  return useQuery({ queryKey: usersQueryKey, queryFn: usersApi.listUsers });
}

export function useCreateUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateUserRequest) => usersApi.createUser(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: usersQueryKey }),
  });
}

export function useUpdateUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateUserRequest }) => usersApi.updateUser(id, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: usersQueryKey }),
  });
}

export function useEnableUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => usersApi.enableUser(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: usersQueryKey }),
  });
}

export function useDisableUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => usersApi.disableUser(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: usersQueryKey }),
  });
}
