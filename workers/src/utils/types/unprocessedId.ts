import { HttpStatusError } from "../offerFetcher.js";

export type failedIds = {
  id: number;
  reason: HttpStatusError | string;
}