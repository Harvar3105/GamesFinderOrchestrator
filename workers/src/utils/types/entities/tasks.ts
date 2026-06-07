import { eCurrency } from "../enums/eCurrency.js";

export interface Task {
  taskId: string;
  createdAt: string;
  updateExistingDeals: boolean;
  redisResultKey: string;
}

export interface SteamTask extends Task {
  gameIds: number[];
  updateExistingGames: boolean;
  updateExistingDeals: boolean;
}

export interface InstantGamingTask extends Task {
  proxy?: string;
  currency?: eCurrency;
  vendorsIds: number[];
}

function normalizeNumberArray(value: unknown): number[] {
  if (!Array.isArray(value)) return [];

  return value
    .map((item) => Number(item))
    .filter((item) => Number.isFinite(item));
}

function normalizeBaseTask(raw: any): Task {
  return {
    taskId: raw.TaskId ?? raw.taskId,
    createdAt: raw.CreatedAt ?? raw.createdAt,
    updateExistingDeals: raw.UpdateExisting ?? raw.updateExisting,
    redisResultKey: raw.RedisResultKey ?? raw.redisResultKey
  }
}

export function normalizeInstantGamingTask(raw: any): InstantGamingTask {
  return {
    ...normalizeBaseTask(raw),
    proxy: raw.Proxy ?? raw.proxy,
    currency: raw.Currency ?? raw.currency,
    vendorsIds: normalizeNumberArray(raw.VendorsIds ?? raw.vendorsIds)
  };
}

export function normalizeSteamTask(raw: any): SteamTask {
  return {
    ...normalizeBaseTask(raw),
    updateExistingGames: raw.UpdateExistingGames ?? raw.updateExistingGames,
    updateExistingDeals: raw.UpdateExistingDeals ?? raw.updateExistingDeals,
    gameIds: normalizeNumberArray(raw.GameIds ?? raw.gameIds)
  };
}

