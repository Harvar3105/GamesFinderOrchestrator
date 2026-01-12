import { Channel, ConsumeMessage } from "amqplib";
import { normalizeSteamTask, normalizeInstantGamingIdsTask, getInstantGamingTaskType, normalizeInstantGamingUpToTask, normalizeInstantGamingRangeTask } from "./types/entities/tasks.js";
import logger from './logger.js';
import { eInstantGamingTaskType } from "./types/enums/eInstantGamingTaskType.js";

export enum TaskKind {
  Steam,
  InstantGaming
}

export function parseTask(msg: ConsumeMessage, type: TaskKind, channel: Channel) {
  const obj = JSON.parse(msg.content.toString());
  let result = null;
  try{
    switch (type) {
      case TaskKind.Steam: {
        result = normalizeSteamTask(obj);
        break;
      }
      case TaskKind.InstantGaming: {
        const type = getInstantGamingTaskType(obj);
        switch (type) {
          case eInstantGamingTaskType.SCRAPE_IDS:
            result = normalizeInstantGamingIdsTask(obj, type);
            break;
          case eInstantGamingTaskType.SCRAPE_RANGE:
            result = normalizeInstantGamingRangeTask(obj, type);
            break;
          case eInstantGamingTaskType.SCRAPE_UP_TO:
            result = normalizeInstantGamingUpToTask(obj, type);
            break;
          default:
            logger.error('❌Unknown InstantGaming task type:', msg.content.toString());
            channel.nack(msg, false, false);
            result = null;
            break;
        }
        break;
      }
    }
  } catch (e) {
    logger.error('❌Invalid JSON from queue:', msg.content.toString());
    channel.nack(msg, false, false);
  }
  
  return result;
}