export type UserRole = "Administrator" | "Trustee";

export interface UserDto {
  id: string;
  email: string;
  displayName: string;
  role: UserRole;
  isEnabled: boolean;
  createdAtUtc: string;
  lastLoginAtUtc: string | null;
}
