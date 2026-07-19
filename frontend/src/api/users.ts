import type { UserDto, UserRole } from "../features/auth/types";
import { apiFetch } from "./client";

export interface CreateUserRequest {
  email: string;
  displayName: string;
  role: UserRole;
}

export interface UpdateUserRequest {
  displayName: string;
  role: UserRole;
}

export function listUsers() {
  return apiFetch<UserDto[]>("/api/users");
}

export function createUser(request: CreateUserRequest) {
  return apiFetch<UserDto>("/api/users", { method: "POST", body: JSON.stringify(request) });
}

export function updateUser(id: string, request: UpdateUserRequest) {
  return apiFetch<UserDto>(`/api/users/${id}`, { method: "PUT", body: JSON.stringify(request) });
}

export function enableUser(id: string) {
  return apiFetch<void>(`/api/users/${id}/enable`, { method: "PATCH" });
}

export function disableUser(id: string) {
  return apiFetch<void>(`/api/users/${id}/disable`, { method: "PATCH" });
}
