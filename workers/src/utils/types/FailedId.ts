import { HttpStatusError } from "../fetch.js";

export type FailedId = {
  id: number;
  reason: HttpStatusError | string;
}