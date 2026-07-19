import { useState } from "react";
import { useAuth } from "../auth/AuthContext";
import type { UserDto } from "../auth/types";
import { AddUserDialog } from "./AddUserDialog";
import { EditUserDialog } from "./EditUserDialog";
import { useCreateUser, useDisableUser, useEnableUser, useUpdateUser, useUsers } from "./useUsers";

export function UsersListPage() {
  const { data: users, isLoading, error } = useUsers();
  const { user: currentUser } = useAuth();
  const createUser = useCreateUser();
  const updateUser = useUpdateUser();
  const enableUser = useEnableUser();
  const disableUser = useDisableUser();

  const [isAdding, setIsAdding] = useState(false);
  const [editingUser, setEditingUser] = useState<UserDto | null>(null);

  if (isLoading) {
    return <p>Loading users…</p>;
  }

  if (error) {
    return <p role="alert">Failed to load users.</p>;
  }

  return (
    <section>
      <div className="users-page__header">
        <h2>Users</h2>
        <button type="button" onClick={() => setIsAdding(true)}>
          Add user
        </button>
      </div>

      <table>
        <thead>
          <tr>
            <th>Email</th>
            <th>Name</th>
            <th>Role</th>
            <th>Status</th>
            <th>Last login</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {users?.map((u) => (
            <tr key={u.id}>
              <td>{u.email}</td>
              <td>{u.displayName}</td>
              <td>{u.role}</td>
              <td>{u.isEnabled ? "Enabled" : "Disabled"}</td>
              <td>{u.lastLoginAtUtc ? new Date(u.lastLoginAtUtc).toLocaleString() : "Never"}</td>
              <td>
                <button type="button" onClick={() => setEditingUser(u)}>
                  Edit
                </button>
                {u.isEnabled ? (
                  <button
                    type="button"
                    onClick={() => disableUser.mutate(u.id)}
                    disabled={u.id === currentUser?.id}
                    title={u.id === currentUser?.id ? "You can't disable your own account" : undefined}
                  >
                    Disable
                  </button>
                ) : (
                  <button type="button" onClick={() => enableUser.mutate(u.id)}>
                    Enable
                  </button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {isAdding && (
        <AddUserDialog
          onClose={() => setIsAdding(false)}
          onSubmit={async (values) => {
            await createUser.mutateAsync(values);
          }}
        />
      )}

      {editingUser && (
        <EditUserDialog
          user={editingUser}
          onClose={() => setEditingUser(null)}
          onSubmit={async (values) => {
            await updateUser.mutateAsync({ id: editingUser.id, request: values });
          }}
        />
      )}
    </section>
  );
}
