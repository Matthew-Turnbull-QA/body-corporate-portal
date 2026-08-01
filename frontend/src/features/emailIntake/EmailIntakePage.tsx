import { Link } from "react-router-dom";
import { useEmailIntakeMessages, usePollEmailIntakeNow } from "./useEmailIntake";

function formatDateTime(value: string) {
  return new Date(value).toLocaleString();
}

export function EmailIntakePage() {
  const { data: messages, isLoading, error } = useEmailIntakeMessages();
  const pollNow = usePollEmailIntakeNow();

  return (
    <section className="page-card">
      <div className="page-header">
        <h2 className="page-title">Email Intake</h2>
        <button
          className="button button--primary"
          type="button"
          onClick={() => pollNow.mutate()}
          disabled={pollNow.isPending}
        >
          Check now
        </button>
      </div>

      {pollNow.data && (
        <p className="text-muted">
          Last check: fetched {pollNow.data.fetched}, created {pollNow.data.created}, skipped{" "}
          {pollNow.data.duplicatesSkipped}, failed {pollNow.data.failed}.
        </p>
      )}

      {pollNow.isError && (
        <p className="error-banner" role="alert">
          {pollNow.error instanceof Error ? pollNow.error.message : "Failed to check email intake."}
        </p>
      )}

      {isLoading && <div className="state-card">Loading email intake messages...</div>}

      {error && (
        <p className="error-banner" role="alert">
          {error instanceof Error ? error.message : "Failed to load email intake messages."}
        </p>
      )}

      {!isLoading && !error && (
        <div className="table-card">
          <table>
            <thead>
              <tr>
                <th>Status</th>
                <th>Sender</th>
                <th>Subject</th>
                <th>Received</th>
                <th>Job</th>
                <th>Failure</th>
              </tr>
            </thead>
            <tbody>
              {messages && messages.length > 0 ? (
                messages.map((message) => (
                  <tr key={message.id}>
                    <td>{message.status}</td>
                    <td>{message.senderDisplayName ?? message.senderEmail}</td>
                    <td>{message.subject}</td>
                    <td>{formatDateTime(message.receivedAtUtc)}</td>
                    <td>{message.jobId ? <Link to="/jobs">Open jobs</Link> : "-"}</td>
                    <td>{message.failureReason ?? "-"}</td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={6} className="table-empty-cell">
                    No processed email yet.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
