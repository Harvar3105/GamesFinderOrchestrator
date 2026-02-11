import { eCurrency } from "../enums/eCurrency.js";
import { eInstantGamingTaskType } from "../enums/eInstantGamingTaskType.js";

export interface Task {
  taskId: string;
  createdAt: string;
  updateExistingDeals: boolean;
  redisResultKey: string;
}

export interface SteamTask extends Task {
  gameIds: number[];
  updateExistingGames: boolean;
}

export interface InstantGamingTask extends Task {
  proxy?: string;
  currency?: eCurrency;
  type: eInstantGamingTaskType | null;
}

export interface InstantGamingScrapeIdsTask extends InstantGamingTask {
  gameIds: number[];
}

export interface InstantGamingScrapeRangeTask extends InstantGamingTask {
  startId: number;
  endId: number;
}

export interface InstantGamingScrapeUpToTask extends InstantGamingTask {
  upToId: number;
}

function normalizeBaseTask(raw: any): Task {
  return {
    taskId: raw.TaskId ?? raw.taskId,
    createdAt: raw.CreatedAt ?? raw.createdAt,
    updateExistingDeals: raw.UpdateExisting ?? raw.updateExisting,
    redisResultKey: raw.RedisResultKey ?? raw.redisResultKey
  }
}

export function getInstantGamingTaskType(raw: any): eInstantGamingTaskType | null {
  if (raw. gameIds !== undefined || raw.GameIds !== undefined) {
    return eInstantGamingTaskType.SCRAPE_IDS;
  } else if (raw.startId !== undefined || raw.StartId !== undefined) {
    return eInstantGamingTaskType.SCRAPE_RANGE;
  } else if (raw.upToId !== undefined || raw.UpToId !== undefined) {
    return eInstantGamingTaskType.SCRAPE_UP_TO;
  }
  return null;
}

export function normalizeSteamTask(raw: any): SteamTask {
  return {
    ...normalizeBaseTask(raw),
    updateExistingGames: raw.UpdateExistingGames ?? raw.updateExistingGames,
    gameIds: raw.GameIds ?? raw.gameIds
  };
}

export function normalizeInstantGamingIdsTask(raw: any, type: eInstantGamingTaskType | null): InstantGamingScrapeIdsTask {
  return {
    ...normalizeBaseTask(raw),
    type: type,
    gameIds: raw.GameIds ?? raw.gameIds,
    currency: raw.Currency ?? raw.currency
  }
}

export function normalizeInstantGamingRangeTask(raw: any, type: eInstantGamingTaskType | null): InstantGamingScrapeRangeTask {
  return {
    ...normalizeBaseTask(raw),
    type: type,
    startId: raw.StartId ?? raw.startId,
    endId: raw.EndId ?? raw.endId,
    currency: raw.Currency ?? raw.currency
  }
}

export function normalizeInstantGamingUpToTask(raw: any, type: eInstantGamingTaskType | null): InstantGamingScrapeUpToTask {
  normalizeBaseTask(raw);
  return {
    ...normalizeBaseTask(raw),
    type: type,
    upToId: raw.UpToId ?? raw.upToId,
    currency: raw.Currency ?? raw.currency
  }
}
