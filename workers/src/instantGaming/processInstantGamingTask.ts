import { config } from "../utils/config.js";
import logger from "../utils/logger.js";
import { HttpStatusError } from "../utils/offerFetcher.js";
import { GameOffer } from "../utils/types/entities/gameOffer.js";
import { InstantGamingTask } from "../utils/types/entities/tasks.js";
import { eCurrency } from "../utils/types/enums/eCurrency.js";
import { fetchInstantGamingOffer } from "./instantGamingFetcher.js";

export default async function processInstantGamingTask(task: InstantGamingTask, msg: any): Promise<GameOffer[]> {
  const { unprocessed, processed } = await processInstantGamingTaskIds(task.vendorsIds, task.taskId, task.currency, task.proxy);
  let totalProcessed = processed;
  let totalUnprocessed = unprocessed;

  if (unprocessed.length > 0) {
    await new Promise(res => setTimeout(res, config.cooldownMs));
    const { unprocessed: unprocessed2, processed: processed2 } = await processInstantGamingTaskIds(unprocessed.map(un => un.id), task.taskId, task.currency, task.proxy);
    totalProcessed = [...totalProcessed, ...processed2];
    totalUnprocessed = unprocessed2;
  }

  if (totalUnprocessed.length > 0) {
    for (const unprocessedItem of totalUnprocessed) {
      logger.warn(`⚠️Offer ${unprocessedItem.id} could not be processed for task ${task.taskId}:`, unprocessedItem.reason);
    }
  }

  return totalProcessed;
}
export type ProcessInstantGamingTaskResult = {
  unprocessed: UnprocessedInstantGamingId[];
  processed: GameOffer[];
};
export type UnprocessedInstantGamingId = {
  id: number;
  reason: HttpStatusError | string;
}

export async function processInstantGamingTaskIds(ids: number[], taskId: string, currency?: eCurrency, proxy?: string): Promise<ProcessInstantGamingTaskResult> {
  const unprocessed = [];
  const processed = [];
  for (const offerId of ids){
    var offer = await fetchInstantGamingOffer(offerId, currency, proxy);
    if (offer instanceof HttpStatusError) {
      unprocessed.push({ id: offerId, reason: offer });
      continue;
    }
    if (offer) processed.push(offer);
  }

  return { unprocessed, processed };
}