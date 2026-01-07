import { redis } from "./config.js";
import logger from "./logger.js";
import { Channel, ConsumeMessage } from "amqplib";

export async function createOrchestratorListener(
  channel: Channel,
  requestsQueue: string,
  resultsQueue: string,
  processTask: (msg: ConsumeMessage | null) => Promise<void>
) {
  await channel.assertQueue(requestsQueue, { durable: true });
  logger.info(`✅Worker listening on queue "${requestsQueue}"...`);
  await channel.assertQueue(resultsQueue, { durable: true });
  logger.info(`✅Worker will publish results to "${resultsQueue}"...`);

  await channel.consume(requestsQueue, processTask);
}

export async function clearRedisKeyIfExists(key: string) {
  if (await redis.exists(key)) {
    await redis.del(key);
    logger.warn(`⚠️Cleared existing Redis key from previous request: ${key}`);
  }
}