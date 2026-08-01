import { createBrowserRouter } from "react-router-dom";
import { LoginPage } from "../features/auth/LoginPage";
import { RequestAccessPage } from "../features/accessRequests/RequestAccessPage";
import { AccessRequestsPage } from "../features/accessRequests/AccessRequestsPage";
import { RequireAuth } from "../features/auth/RequireAuth";
import { RequirePortalAdmin } from "../features/auth/RequireRole";
import { UsersListPage } from "../features/users/UsersListPage";
import { PropertiesListPage } from "../features/properties/PropertiesListPage";
import { JobsListPage } from "../features/jobs/JobsListPage";
import { EmailIntakePage } from "../features/emailIntake/EmailIntakePage";
import { AssignmentRulesPage } from "../features/assignmentRules/AssignmentRulesPage";
import { NotificationsPage } from "../features/notifications/NotificationsPage";
import { AppLayout } from "./AppLayout";
import { HomePage } from "./HomePage";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <AppLayout />,
    children: [
      { path: "login", element: <LoginPage /> },
      { path: "request-access", element: <RequestAccessPage /> },
      {
        element: <RequireAuth />,
        children: [
          { index: true, element: <HomePage /> },
          { path: "properties", element: <PropertiesListPage /> },
          { path: "jobs", element: <JobsListPage /> },
          { path: "notifications", element: <NotificationsPage /> },
          {
            element: <RequirePortalAdmin />,
            children: [
              { path: "users", element: <UsersListPage /> },
              { path: "access-requests", element: <AccessRequestsPage /> },
              { path: "email-intake", element: <EmailIntakePage /> },
              { path: "assignment", element: <AssignmentRulesPage /> },
            ],
          },
        ],
      },
    ],
  },
]);
