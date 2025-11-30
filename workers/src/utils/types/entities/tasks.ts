import { eCurrency } from "../enums/eCurrency.js";

export interface Task {
  taskId: string;
  createdAt: string;
  updateExisting: boolean;
  redisResultKey: string;
}

export interface SteamTask extends Task {
  gameIds: number[];
}

export interface InstantGamingTask extends Task {
  gameIds: number[];
  proxy?: string;
  currency?: eCurrency;
}

export function normalizeSteamTask(raw: any): SteamTask {
  return {
    taskId: raw.TaskId ?? raw.taskId,
    gameIds: raw.GameIds ?? raw.gameIds,
    updateExisting: raw.UpdateExisting ?? raw.updateExisting,
    redisResultKey: raw.RedisResultKey ?? raw.redisResultKey,
    createdAt: raw.CreatedAt ?? raw.createdAt
  };
}