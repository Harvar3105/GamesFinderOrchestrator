import { config } from "../utils/config.js";
import logger from "../utils/logger.js";
import { HttpStatusError, WrongDataError } from "../utils/offerFetcher.js";
import { Game, isGame } from "../utils/types/entities/game.js";
import { GameOffer, isGameOffer } from "../utils/types/entities/gameOffer.js";
import { FailedId } from "../utils/types/FailedId.js";
import { fetchSteamGame, SkippedError } from "./gameFetcher.js";

export type SteamWorkerTaskResult = {
  games: Game[];
  offers: GameOffer[];
}

export default async function processSteamWorkerTask(batch: number[], taskId: string, updateExistingGames: boolean, updateExistingDeals: boolean): Promise<SteamWorkerTaskResult> {
  let callsCounter = 0;
  let games: Game[] = [];
  let offers: GameOffer[] = [];

  const processResult = await processSteamWorkerIds(batch, updateExistingGames, updateExistingDeals, callsCounter);
  games = processResult.games;
  offers = processResult.offers;
  callsCounter = processResult.newCallsState;

  const retry = await processSteamWorkerIds(processResult.failedIds.map(f => f.id), updateExistingGames, updateExistingDeals, processResult.newCallsState);
  games = games.concat(retry.games);
  offers = offers.concat(retry.offers);

  if (retry.failedIds.length > 0) {
    for (const failed of retry.failedIds) {
      logger.warn(`⚠️Offer ${failed.id} could not be processed for task ${taskId}:`, failed.reason);
    }
  }

  return { games, offers };
}

export type SteamWorkerProcessResult = {
  games: Game[];
  offers: GameOffer[];
  failedIds: FailedId[];
  newCallsState: number;
}

export async function processSteamWorkerIds(ids: number[], updateExistingGames: boolean, updateExistingDeals: boolean, callsCounter: number): Promise<SteamWorkerProcessResult> {
  let games: Game[] = [];
  let offers: GameOffer[] = [];
  let failedIds: FailedId[] = [];

  for (const id of ids) {
    let fetchResult;
    try {
      callsCounter++;
      fetchResult = await fetchSteamGame(id, updateExistingGames, updateExistingDeals);
      await new Promise(res => setTimeout(res, config.steamTagsAndGenresRequestDelayMs));
    } catch (err) {
      if (err instanceof HttpStatusError && err.status === 429) {
        logger.info(`⌚Rate limited while fetching game ${id}. Waiting for ${config.cooldownMs}ms before retrying...`);
        await new Promise(res => setTimeout(res, config.cooldownMs));
        callsCounter++;
        try {
          fetchResult = await fetchSteamGame(id, updateExistingGames, updateExistingDeals);
          await new Promise(res => setTimeout(res, config.steamTagsAndGenresRequestDelayMs));
        } catch (err) {
          if (err instanceof HttpStatusError) {
            logger.error(err.message);
            failedIds.push({id: id, reason: err.message});
          }
          continue;
        }
      } else if (err instanceof HttpStatusError) {
        logger.error(err.message);
        failedIds.push({id: id, reason: err.message});
        continue;
      } else if (err instanceof WrongDataError || err instanceof SkippedError) {
        logger.error(err.message);
        continue;
      }
    }

    if (isGame(fetchResult)) games.push(fetchResult);
    else if (isGameOffer(fetchResult)) offers.push(fetchResult);
  }

  return { games, offers, failedIds, newCallsState: callsCounter };
}