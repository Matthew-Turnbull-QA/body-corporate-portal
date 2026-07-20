import { createBrowserRouter } from "react-router-dom";
import { LoginPage } from "../features/auth/LoginPage";
import { RequireAuth } from "../features/auth/RequireAuth";
import { RequireRole } from "../features/auth/RequireRole";
import { UsersListPage } from "../features/users/UsersListPage";
import { PropertiesListPage } from "../features/properties/PropertiesListPage";
import { JobsListPage } from "../features/jobs/JobsListPage";
import { AppLayout } from "./AppLayout";
import { HomePage } from "./HomePage";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <AppLayout />,
    children: [
      { path: "login", element: <LoginPage /> },
      {
        element: <RequireAuth />,
        children: [
          { index: true, element: <HomePage /> },
          { path: "properties", element: <PropertiesListPage /> },
          { path: "jobs", element: <JobsListPage /> },
          {
            element: <RequireRole role="Administrator" />,
            children: [{ path: "users", element: <UsersListPage /> }],
          },
        ],
      },
    ],
  },
]);
