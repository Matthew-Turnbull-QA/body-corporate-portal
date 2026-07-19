import { apiFetch } from "./client";

export interface PropertyDto {
  id: string;
  name: string;
  addressLine1: string;
  suburb: string;
  state: string;
  postcode: string;
  createdAtUtc: string;
}

export interface CreatePropertyRequest {
  name: string;
  addressLine1: string;
  suburb: string;
  state: string;
  postcode: string;
}

export function listProperties() {
  return apiFetch<PropertyDto[]>("/api/properties");
}

export function createProperty(request: CreatePropertyRequest) {
  return apiFetch<PropertyDto>("/api/properties", { method: "POST", body: JSON.stringify(request) });
}
