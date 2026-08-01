export interface UserDto {
  id: string;
  email: string;
  displayName: string;
  isPortalAdmin: boolean;
  isSystem: boolean;
  hasLocalPassword: boolean;
  isEnabled: boolean;
  createdAtUtc: string;
  lastLoginAtUtc: string | null;
}
