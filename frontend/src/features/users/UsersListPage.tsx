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

  return (
    <section className="page-card">
      <div className="page-header">
        <h2 className="page-title">Users</h2>
        <button className="button button--primary" type="button" onClick={() => setIsAdding(true)}>
          Add user
        </button>
      </div>

      {isLoading && <div className="state-card">Loading users…</div>}

      {error && (
        <p className="error-banner" role="alert">
          Failed to load users.
        </p>
      )}

      {!isLoading && !error && (
        <div className="table-card">
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
                  <td>
                    <span className={`status-chip ${u.isEnabled ? "status-chip--enabled" : "status-chip--disabled"}`}>
                      {u.isEnabled ? "Enabled" : "Disabled"}
                    </span>
                  </td>
                  <td>{u.lastLoginAtUtc ? new Date(u.lastLoginAtUtc).toLocaleString() : "Never"}</td>
                  <td>
                    <div className="table-actions">
                      <button className="button button--ghost" type="button" onClick={() => setEditingUser(u)}>
                        Edit
                      </button>
                      {u.isEnabled ? (
                        <button
                          className={`button ${u.id === currentUser?.id ? "button--muted" : "button--danger"}`}
                          type="button"
                          onClick={() => disableUser.mutate(u.id)}
                          disabled={u.id === currentUser?.id}
                          title={u.id === currentUser?.id ? "You can't disable your own account" : undefined}
                        >
                          Disable
                        </button>
                      ) : (
                        <button className="button button--ghost" type="button" onClick={() => enableUser.mutate(u.id)}>
                          Enable
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

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
