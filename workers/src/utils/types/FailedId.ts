import { HttpStatusError } from "../offerFetcher.js";

export type FailedId = {
  id: number;
  reason: HttpStatusError | string;
}