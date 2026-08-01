import { useNotifications } from "./useNotifications";

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

export function NotificationsPage() {
  const { data: notifications, isLoading, error } = useNotifications();

  return (
    <section className="page-card">
      <div className="page-header">
        <h2 className="page-title">Notifications</h2>
      </div>

      {isLoading && <div className="state-card">Loading notifications...</div>}
      {error && <p className="error-banner">Failed to load notifications.</p>}

      {!isLoading && !error && (
        <div className="notification-list">
          {notifications?.length === 0 ? (
            <div className="state-card">No notifications yet.</div>
          ) : (
            notifications?.map((notification) => (
              <article className="notification-item" key={notification.id}>
                <div>
                  <h3>{notification.subject}</h3>
                  <p>{notification.message}</p>
                  {notification.jobNumber && <span className="text-muted">Job #{notification.jobNumber}</span>}
                  {notification.emailFailureReason && (
                    <p className="table-warning-text">Email not sent: {notification.emailFailureReason}</p>
                  )}
                </div>
                <time>{formatDate(notification.createdAtUtc)}</time>
              </article>
            ))
          )}
        </div>
      )}
    </section>
  );
}
