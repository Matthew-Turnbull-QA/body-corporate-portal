import { useState } from "react";
import type { AccessRequestDto, AccessRequestRelationship } from "../../api/accessRequests";
import { useAccessRequests, useApproveAccessRequest, useRejectAccessRequest } from "./useAccessRequests";

const relationshipLabels: Record<AccessRequestRelationship, string> = {
  Trustee: "Trustee",
  Owner: "Owner",
  Resident: "Resident",
  ManagingAgent: "Managing agent",
  Contractor: "Contractor",
  Other: "Other",
};

const statusChipClass = {
  Pending: "status-chip--inprogress",
  Approved: "status-chip--enabled",
  Rejected: "status-chip--cancelled",
};

function AccessRequestRow({ request }: { request: AccessRequestDto }) {
  const approveAccessRequest = useApproveAccessRequest();
  const rejectAccessRequest = useRejectAccessRequest();
  const [isPortalAdmin, setIsPortalAdmin] = useState(false);
  const [password, setPassword] = useState("");
  const [reviewNote, setReviewNote] = useState("");

  const isPending = request.status === "Pending";
  const accountContext = request.existingUserId
    ? request.existingUserIsEnabled
      ? "Existing active account"
      : "Reactivation request"
    : "New account request";

  async function handleApprove() {
    await approveAccessRequest.mutateAsync({
      id: request.id,
      request: {
        isPortalAdmin,
        password: password.trim() || null,
        reviewNote: reviewNote.trim() || null,
      },
    });
  }

  async function handleReject() {
    await rejectAccessRequest.mutateAsync({
      id: request.id,
      reviewNote: reviewNote.trim() || null,
    });
  }

  return (
    <tr>
      <td>
        <strong>{request.displayName}</strong>
        <span className="table-subtext">{request.email}</span>
        <span className="table-subtext">{request.phoneNumber}</span>
        <span className="table-subtext table-subtext--strong">{accountContext}</span>
      </td>
      <td>
        {request.propertyOrUnit}
        <span className="table-subtext">{relationshipLabels[request.relationship]}</span>
      </td>
      <td>{request.message || "None"}</td>
      <td>{new Date(request.createdAtUtc).toLocaleString()}</td>
      <td>
        <span className={`status-chip ${statusChipClass[request.status]}`}>{request.status}</span>
        {request.reviewNote && <span className="table-subtext">{request.reviewNote}</span>}
      </td>
      <td>
        {isPending ? (
          <div className="review-controls">
            <label className="checkbox-option">
              <input
                type="checkbox"
                checked={isPortalAdmin}
                onChange={(event) => setIsPortalAdmin(event.target.checked)}
              />
              Portal admin
            </label>
            <input
              type="password"
              minLength={8}
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              placeholder="Optional local password"
            />
            <textarea
              rows={2}
              value={reviewNote}
              onChange={(event) => setReviewNote(event.target.value)}
              placeholder="Review note"
            />
            <div className="table-actions">
              <button
                className="button button--primary"
                type="button"
                onClick={handleApprove}
                disabled={approveAccessRequest.isPending || rejectAccessRequest.isPending}
              >
                {request.existingUserId && !request.existingUserIsEnabled ? "Reactivate" : "Approve"}
              </button>
              <button
                className="button button--danger"
                type="button"
                onClick={handleReject}
                disabled={approveAccessRequest.isPending || rejectAccessRequest.isPending}
              >
                Reject
              </button>
            </div>
          </div>
        ) : (
          <span className="text-muted">
            {request.reviewedAtUtc ? new Date(request.reviewedAtUtc).toLocaleString() : "Reviewed"}
          </span>
        )}
      </td>
    </tr>
  );
}

export function AccessRequestsPage() {
  const { data: requests, isLoading, error } = useAccessRequests();

  return (
    <section className="page-card">
      <div className="page-header">
        <h2 className="page-title">Access requests</h2>
      </div>

      {isLoading && <div className="state-card">Loading access requests...</div>}

      {error && (
        <p className="error-banner" role="alert">
          Failed to load access requests.
        </p>
      )}

      {!isLoading && !error && (
        <div className="table-card">
          <table>
            <thead>
              <tr>
                <th>Person</th>
                <th>Property</th>
                <th>Message</th>
                <th>Submitted</th>
                <th>Status</th>
                <th>Review</th>
              </tr>
            </thead>
            <tbody>
              {requests && requests.length > 0 ? (
                requests.map((request) => <AccessRequestRow key={request.id} request={request} />)
              ) : (
                <tr>
                  <td colSpan={6} className="table-empty-cell">
                    No access requests.
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
