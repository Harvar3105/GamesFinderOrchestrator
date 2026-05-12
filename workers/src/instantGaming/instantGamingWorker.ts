import { rabbitConn } from "../utils/config.js";
import { config, redis } from "../utils/config.js";
import logger from "../utils/logger.js";
import { HttpStatusError } from "../utils/offerFetcher.js";
import { clearRedisKeyIfExists, createOrchestratorListener } from "../utils/orchestratorListener.js";
import { parseTask, TaskKind } from "../utils/taskParser.js";
import { InstantGamingTask } from "../utils/types/entities/tasks.js";
import { eInstantGamingTaskType } from "../utils/types/enums/eInstantGamingTaskType.js";
import { fetchInstantGamingOffer } from "./instantGamingFetcher.js";

async function startInstantGamingWorker() {
  const channel = await rabbitConn.createChannel();
  await channel.prefetch(1);
  await createOrchestratorListener(
    channel,
    config.instantGamingRequests!,
    config.instantGamingResults!,
    async (msg) => {
      if (!msg) return;

      let task: InstantGamingTask | null = parseTask(msg, TaskKind.InstantGaming, channel) as InstantGamingTask | null; // We know it wont be Steam type
      if (!task) return;

      try {
        const unprocessedIds = [];
        const list = [];
        for (const offerId of task.vendorsIds){
          var offer = await fetchInstantGamingOffer(offerId, task.currency, task.proxy);
          if (offer instanceof HttpStatusError) {
            logger.warn(`⚠️Error fetching offer ${offerId} for task ${task.taskId}:`, offer);
            unprocessedIds.push(offerId);
            continue;
          }
          if (offer) list.push(offer);
        }

        if (unprocessedIds.length > 0) {
          await new Promise(res => setTimeout(res, config.cooldownMs));
          for (const offerId of unprocessedIds){
            var offer = await fetchInstantGamingOffer(offerId, task.currency, task.proxy);
            if (offer instanceof HttpStatusError) {
              logger.error(`💥Error fetching offer ${offerId} for second time. Requires further developers investigation!`, offer);
              continue;
            }
            if (offer) list.push(offer);
          }
        }

        if (list.length === 0) {
          logger.info(`⚠️No offers found for task ${task.taskId}, acknowledging without adding to redis.`);
          channel.ack(msg);
          return;
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
    }
  )
}

startInstantGamingWorker().catch(logger.crit);