import { useEffect, useMemo, useState } from "react";
import type { AssignmentRuleDto, SaveAssignmentRuleRequest } from "../../api/assignmentRules";
import type { JobSource } from "../../api/jobs";
import { useProperties } from "../properties/useProperties";
import { useAssignableTrustees } from "../users/useUsers";
import {
  useAssignmentRules,
  useCreateAssignmentRule,
  useReorderAssignmentRules,
  useToggleAssignmentRule,
  useUpdateAssignmentRule,
} from "./useAssignmentRules";

const emptyForm = {
  name: "",
  targetTrusteeUserId: "",
  propertyId: "",
  jobSource: "",
  keywords: "",
  isEnabled: true,
};

type RuleForm = typeof emptyForm;

function toForm(rule: AssignmentRuleDto): RuleForm {
  return {
    name: rule.name,
    targetTrusteeUserId: rule.targetTrusteeUserId,
    propertyId: rule.propertyId ?? "",
    jobSource: rule.jobSource ?? "",
    keywords: rule.keywords.join(", "),
    isEnabled: rule.isEnabled,
  };
}

function toRequest(form: RuleForm): SaveAssignmentRuleRequest {
  return {
    name: form.name,
    targetTrusteeUserId: form.targetTrusteeUserId,
    propertyId: form.propertyId || null,
    jobSource: (form.jobSource || null) as JobSource | null,
    keywords: form.keywords.split(",").map((keyword) => keyword.trim()).filter(Boolean),
    isEnabled: form.isEnabled,
  };
}

function criteriaText(rule: AssignmentRuleDto) {
  const criteria = [
    rule.propertyName ? `Property: ${rule.propertyName}` : null,
    rule.jobSource ? `Source: ${rule.jobSource}` : null,
    rule.keywords.length > 0 ? `Keywords: ${rule.keywords.join(", ")}` : null,
  ].filter(Boolean);
  return criteria.length > 0 ? criteria.join(" | ") : "No criteria";
}

export function AssignmentRulesPage() {
  const { data: rules, isLoading, error } = useAssignmentRules();
  const { data: properties } = useProperties();
  const { data: trustees } = useAssignableTrustees(true);
  const createRule = useCreateAssignmentRule();
  const updateRule = useUpdateAssignmentRule();
  const toggleRule = useToggleAssignmentRule();
  const reorderRules = useReorderAssignmentRules();
  const [isAdding, setIsAdding] = useState(false);
  const [editingRuleId, setEditingRuleId] = useState<string | null>(null);
  const [form, setForm] = useState<RuleForm>(emptyForm);

  const sortedRules = useMemo(
    () => [...(rules ?? [])].sort((a, b) => a.priority - b.priority),
    [rules],
  );
  const editingRule = sortedRules.find((rule) => rule.id === editingRuleId) ?? null;
  const isSaving = createRule.isPending || updateRule.isPending;
  const formHasCriteria = Boolean(form.propertyId || form.jobSource || form.keywords.trim());

  useEffect(() => {
    if (editingRule) {
      setForm(toForm(editingRule));
    }
  }, [editingRule]);

  function openAdd() {
    setForm(emptyForm);
    setEditingRuleId(null);
    setIsAdding(true);
  }

  function closeDialog() {
    setIsAdding(false);
    setEditingRuleId(null);
    setForm(emptyForm);
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!form.targetTrusteeUserId || !formHasCriteria) {
      return;
    }

    if (editingRule) {
      await updateRule.mutateAsync({ id: editingRule.id, request: toRequest(form) });
    } else {
      await createRule.mutateAsync(toRequest(form));
    }

    closeDialog();
  }

  async function moveRule(ruleId: string, direction: -1 | 1) {
    const currentIndex = sortedRules.findIndex((rule) => rule.id === ruleId);
    const targetIndex = currentIndex + direction;
    if (currentIndex < 0 || targetIndex < 0 || targetIndex >= sortedRules.length) {
      return;
    }

    const nextOrder = [...sortedRules];
    const [rule] = nextOrder.splice(currentIndex, 1);
    nextOrder.splice(targetIndex, 0, rule);
    await reorderRules.mutateAsync(nextOrder.map((item) => item.id));
  }

  function renderDialog() {
    return (
      <div className="dialog-overlay" role="dialog" aria-modal="true" onClick={closeDialog}>
        <form className="dialog dialog--large" onClick={(event) => event.stopPropagation()} onSubmit={handleSubmit}>
          <h3>{editingRule ? "Edit assignment rule" : "Add assignment rule"}</h3>
          <label className="dialog__field">
            Name
            <input
              value={form.name}
              onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
              required
            />
          </label>
          <label className="dialog__field">
            Assign to
            <select
              value={form.targetTrusteeUserId}
              onChange={(event) => setForm((current) => ({ ...current, targetTrusteeUserId: event.target.value }))}
              required
            >
              <option value="">Select trustee</option>
              {trustees?.map((trustee) => (
                <option key={trustee.id} value={trustee.id}>
                  {trustee.displayName}
                </option>
              ))}
            </select>
          </label>
          <div className="dialog__grid">
            <label className="dialog__field">
              Property
              <select
                value={form.propertyId}
                onChange={(event) => setForm((current) => ({ ...current, propertyId: event.target.value }))}
              >
                <option value="">Any property</option>
                {properties?.map((property) => (
                  <option key={property.id} value={property.id}>
                    {property.name}
                  </option>
                ))}
              </select>
            </label>
            <label className="dialog__field">
              Source
              <select
                value={form.jobSource}
                onChange={(event) => setForm((current) => ({ ...current, jobSource: event.target.value }))}
              >
                <option value="">Any source</option>
                <option value="Manual">Manual</option>
                <option value="Email">Email</option>
              </select>
            </label>
          </div>
          <label className="dialog__field">
            Keywords
            <input
              value={form.keywords}
              onChange={(event) => setForm((current) => ({ ...current, keywords: event.target.value }))}
              placeholder="leak, roof, gate"
            />
          </label>
          <label className="checkbox-row">
            <input
              type="checkbox"
              checked={form.isEnabled}
              onChange={(event) => setForm((current) => ({ ...current, isEnabled: event.target.checked }))}
            />
            Enabled
          </label>
          {!formHasCriteria && <p className="error-banner">Choose at least one criterion.</p>}
          <div className="dialog__actions">
            <button className="button button--ghost" type="button" onClick={closeDialog}>
              Cancel
            </button>
            <button className="button button--primary" type="submit" disabled={isSaving || !formHasCriteria}>
              Save
            </button>
          </div>
        </form>
      </div>
    );
  }

  return (
    <section className="page-card">
      <div className="page-header">
        <h2 className="page-title">Assignment</h2>
        <button className="button button--primary" type="button" onClick={openAdd}>
          Add rule
        </button>
      </div>

      {isLoading && <div className="state-card">Loading assignment rules...</div>}
      {error && <p className="error-banner">Failed to load assignment rules.</p>}

      {!isLoading && !error && (
        <div className="table-card">
          <table>
            <thead>
              <tr>
                <th>Order</th>
                <th>Rule</th>
                <th>Criteria</th>
                <th>Assign to</th>
                <th>Status</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {sortedRules.length === 0 ? (
                <tr>
                  <td colSpan={6}>No assignment rules yet.</td>
                </tr>
              ) : (
                sortedRules.map((rule, index) => (
                  <tr key={rule.id}>
                    <td>#{rule.priority}</td>
                    <td>{rule.name}</td>
                    <td>{criteriaText(rule)}</td>
                    <td>{rule.targetTrusteeName}</td>
                    <td>{rule.isEnabled ? "Enabled" : "Disabled"}</td>
                    <td>
                      <div className="table-actions">
                        <button className="button button--ghost" type="button" onClick={() => moveRule(rule.id, -1)} disabled={index === 0}>
                          Up
                        </button>
                        <button className="button button--ghost" type="button" onClick={() => moveRule(rule.id, 1)} disabled={index === sortedRules.length - 1}>
                          Down
                        </button>
                        <button className="button button--ghost" type="button" onClick={() => setEditingRuleId(rule.id)}>
                          Edit
                        </button>
                        <button
                          className="button button--ghost"
                          type="button"
                          onClick={() => toggleRule.mutate({ id: rule.id, isEnabled: !rule.isEnabled })}
                        >
                          {rule.isEnabled ? "Disable" : "Enable"}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}

      {(isAdding || editingRule) && renderDialog()}
    </section>
  );
}
