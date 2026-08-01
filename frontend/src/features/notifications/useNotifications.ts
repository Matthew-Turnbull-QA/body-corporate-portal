import { useQuery } from "@tanstack/react-query";
import * as notificationsApi from "../../api/notifications";

export function useNotifications() {
  return useQuery({ queryKey: ["notifications"], queryFn: notificationsApi.listNotifications });
}
