import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as propertiesApi from "../../api/properties";
import type { CreatePropertyRequest } from "../../api/properties";

const propertiesQueryKey = ["properties"] as const;

export function useProperties() {
  return useQuery({ queryKey: propertiesQueryKey, queryFn: propertiesApi.listProperties });
}

export function useCreateProperty() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreatePropertyRequest) => propertiesApi.createProperty(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: propertiesQueryKey }),
  });
}
