export type UserRole = "Administrator" | "Trustee";
export type UserPermission = "LoadJobs" | "CreateJobs" | "UpdateJobStatus" | "AssignJobs";

export const jobPermissionLabels: Record<UserPermission, string> = {
  LoadJobs: "Load jobs",
  CreateJobs: "Create jobs",
  UpdateJobStatus: "Update status",
  AssignJobs: "Assign jobs",
};

export const allJobPermissions = Object.keys(jobPermissionLabels) as UserPermission[];

export function defaultPermissionsForRole(role: UserRole): UserPermission[] {
  return role === "Administrator" ? [...allJobPermissions] : ["LoadJobs", "CreateJobs", "UpdateJobStatus"];
}

export function hasPermission(user: UserDto | null | undefined, permission: UserPermission): boolean {
  return user?.permissions.includes(permission) ?? false;
}

export interface UserDto {
  id: string;
  email: string;
  displayName: string;
  role: UserRole;
  permissions: UserPermission[];
  hasLocalPassword: boolean;
  isEnabled: boolean;
  createdAtUtc: string;
  lastLoginAtUtc: string | null;
}
