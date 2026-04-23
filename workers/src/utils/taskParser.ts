import { Channel, ConsumeMessage } from "amqplib";
import { normalizeSteamTask, normalizeInstantGamingTask } from "./types/entities/tasks.js";
import logger from './logger.js';

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
        result = normalizeInstantGamingTask(obj);
        break;
      }
    }
  } catch (e) {
    logger.error('❌Invalid JSON from queue:', msg.content.toString());
    channel.nack(msg, false, false);
  }

  return result;
}