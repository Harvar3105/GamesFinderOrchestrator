import { isObject } from "../typeGuards.js";

export interface Entity {
  id: string;
  createdAt: string;
  updatedAt: string;
}

export function isEntity(value: unknown): value is Entity {
  if (!isObject(value)) return false;

  return (
    typeof value.id === "string" &&
    typeof value.createdAt === "string" &&
    typeof value.updatedAt === "string"
  );
}
