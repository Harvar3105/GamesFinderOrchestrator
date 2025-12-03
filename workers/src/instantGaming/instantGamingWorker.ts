import { rabbitConn } from "../utils/config.js";
import { config, redis } from "../utils/config.js";
import logger from "../utils/logger.js";
import { InstantGamingTask } from "../utils/types/entities/tasks.js";
import { fetchInstantGamingOffer } from "./instantGamingFetcher.js";

async function startInstantGamingWorker() {
  const channel = await rabbitConn.createChannel();

  await channel.assertQueue(config.instantGamingRequests!, { durable: true });
  logger.info(`✅InstantGaming worker listening on queue "instantGamingQueue"...`);
  await channel.assertQueue(config.instantGamingResults!, { durable: true });
  logger.info(`✅InstantGaming worker will publish results to "instantGamingResults"...`);

  await channel.consume("instantGamingQueue", async (msg) => {
    if (!msg) return;

    let task: InstantGamingTask;
    try {
      task = JSON.parse(msg.content.toString()) as InstantGamingTask;
    } catch (err) {
      logger.error('❌Invalid JSON from queue:', msg?.content?.toString());
      channel.nack(msg, false, false);
      return;
    }

    if (await redis.exists(task.redisResultKey)) {
      await redis.del(task.redisResultKey);
      logger.warn(`⚠️Cleared existing Redis key from previous request: ${task.redisResultKey}`);
    }

    try {
      const list = [];
      // TODO: Separate to different batches and work in paralel?
      for (const gameId of task.gameIds) {
        // TODO: add ignore existing option
        var offer = await fetchInstantGamingOffer(gameId, task.currency, task.proxy)
        if (offer) list.push(offer);
      }

      await redis.rpush(task.redisResultKey, ...list.map(r => JSON.stringify(r)));
      logger.info(`ℹ️Just added ${list.length} gameOffers to redis from InstantGaming.}`);

      await channel.sendToQueue(
        config.instantGamingResults!,
        Buffer.from(JSON.stringify({
          taskId: task.taskId,
          redisResultKey: task.redisResultKey,
        })),
        { persistent: true }
      );

      channel.ack(msg);
      logger.info(`✅InstantGaming task ${task.taskId} done, created ${list.length} offers.`);
    } catch (err) {
      logger.error('❌Error processing instantGaming task:', err);
      channel.nack(msg, false, true);
    }
  });
}

startInstantGamingWorker().catch(logger.crit);