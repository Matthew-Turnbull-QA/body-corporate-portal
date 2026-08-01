# Manual Test Tracker

Last updated: 2026-08-01

Use this as the living checklist for human/browser testing. Keep automated build/test results in the update logs; this file is for flows that need a real browser, real auth state, real Gmail mailbox, or user judgement.

Status values:

- `PASS`: tested and accepted.
- `TODO`: not tested yet.
- `RETEST`: previously tested, but touched by recent changes and should be checked again.
- `BLOCKED`: cannot test until an account, secret, environment, or dependency is available.

## Current Priority

| Area | Scenario | Status | Last tested | Notes |
| --- | --- | --- | --- | --- |
| Assignment | Portal admin can open `/assignment` and see the rules page | TODO | - | New in Assignment engine slice. |
| Assignment | Portal admin can create a rule with property/source/keyword criteria | TODO | - | Verify validation requires at least one criterion. |
| Assignment | Portal admin can edit a rule | TODO | - | Confirm changes affect future job assignment. |
| Assignment | Portal admin can enable/disable a rule | TODO | - | Disabled rules must not route jobs. |
| Assignment | Portal admin can reorder rules | TODO | - | Higher-priority matching rule should win. |
| Jobs | Manual job matching an enabled rule is assigned to that rule's trustee | TODO | - | Check job row/detail shows `Rule: {rule name}`. |
| Jobs | Manual job with no matching rule falls back to round robin | TODO | - | Check job row/detail shows `Round robin`. |
| Jobs | Portal-admin manual reassignment marks job as manual | TODO | - | Check job row/detail shows `Manual`; automation should not move it later. |
| Email intake + Assignment | Email-created job reroutes when first property/unit is selected | TODO | - | Only if not manually reassigned first. |
| Notifications | Assigned trustee can open `/notifications` and see assignment history | TODO | - | Simple chronological history; no read/unread state. |
| Notifications | Assignment email is sent to assigned/reassigned trustee | TODO | - | Check Gmail delivery and notification email status/failure. |
| Notifications | Previous trustee is notified when job is reassigned away | TODO | - | Verify in-app history and email. |
| Notifications | Routing warning is sent to portal admins when matching rule targets are unavailable | TODO | - | Requires a rule pointing to a disabled trustee. |

## Auth And Access

| Area | Scenario | Status | Last tested | Notes |
| --- | --- | --- | --- | --- |
| Auth | Bootstrap portal admin can sign in with Google | PASS | 2026-07-20 | Verified with real Google consent screen. |
| Auth | Unknown Google account is rejected | TODO | - | Needs a second real Google account not added as a user. |
| Auth | Admin adds a second user, and that user can sign in | TODO | - | Needs a second real Google test-user account. |
| Auth | Disabled user is rejected on next login/API call | TODO | - | Use the second test account after disabling it. |
| Access | Trustee cannot reach portal-admin routes by URL | TODO | - | Check `/users`, `/access-requests`, `/email-intake`, `/assignment`. |
| Access | Portal admin can open Users, Access, Email Intake, Assignment | RETEST | - | Assignment link is new; top nav changed. |

## Properties

| Area | Scenario | Status | Last tested | Notes |
| --- | --- | --- | --- | --- |
| Properties | Portal admin/trustee can list properties | PASS | 2026-07-19 | Previously verified. |
| Properties | Add property works end to end | PASS | 2026-07-19 | Previously verified. |
| Properties | Edit property works end to end | PASS | 2026-07-19 | Previously verified. |
| Properties | Property selector still works in job dialogs | RETEST | - | Assignment engine uses property as a routing signal. |

## Jobs

| Area | Scenario | Status | Last tested | Notes |
| --- | --- | --- | --- | --- |
| Jobs | Jobs list loads and separates Active/Closed | PASS | 2026-07-30 | Retest after assignment-source labels if layout looks off. |
| Jobs | Add manual job works | RETEST | - | Assignment routing now runs during create. |
| Jobs | Job title opens detail modal | PASS | 2026-07-30 | Retest if assignment detail display overlaps. |
| Jobs | Assigned trustee or portal admin can edit job details | RETEST | - | Email job property selection can now trigger rerouting. |
| Jobs | Other trustees can view but not mutate unassigned jobs | RETEST | - | Check access remains intact after assignment changes. |
| Jobs | Status change with note writes audit history | PASS | 2026-07-30 | No direct change, low risk. |
| Jobs | Email-created job cannot leave Open until property/unit selected | PASS | 2026-08-01 | Verified during email-intake manual testing. |
| Jobs | Portal admin can manually assign/unassign trustee | RETEST | - | Now sets/clears assignment provenance. |

## Email Intake

| Area | Scenario | Status | Last tested | Notes |
| --- | --- | --- | --- | --- |
| Email intake | Dedicated Rietvlei Gmail secrets configured | PASS | 2026-08-01 | User verified. |
| Email intake | Real test email creates one Open email job | PASS | 2026-08-01 | User verified. |
| Email intake | Sender receives acknowledgement email | PASS | 2026-08-01 | User verified. |
| Email intake | Enabled trustees are BCC'd on acknowledgement | PASS | 2026-08-01 | User verified. |
| Email intake | Duplicate poll skips already-processed email | PASS | 2026-08-01 | User verified. |
| Email intake | Email-created job shows unit required until property selected | PASS | 2026-08-01 | User verified. |
| Email intake | Email-created job assignment email is sent to routed trustee | TODO | - | New assignment notification behavior. |

## How To Update This File

When you test a scenario:

1. Change `TODO` or `RETEST` to `PASS` if accepted.
2. Set `Last tested` to the date.
3. Add a short note for failures, caveats, or accounts used.
4. If a code change touches a previously passed flow, change it to `RETEST` instead of leaving stale confidence behind.
