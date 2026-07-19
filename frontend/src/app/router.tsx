import { createBrowserRouter } from "react-router-dom";
import { LoginPage } from "../features/auth/LoginPage";
import { RequireAuth } from "../features/auth/RequireAuth";
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
        children: [{ index: true, element: <HomePage /> }],
      },
    ],
  },
]);
