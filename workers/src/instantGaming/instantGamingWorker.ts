import { rabbitConn } from "../utils/config.js";
import { config, redis } from "../utils/config.js";
import logger from "../utils/logger.js";
import { clearRedisKeyIfExists, createOrchestratorListener } from "../utils/orchestratorListener.js";
import { parseTask, TaskKind } from "../utils/taskParser.js";
import { InstantGamingScrapeIdsTask, InstantGamingScrapeRangeTask, InstantGamingTask } from "../utils/types/entities/tasks.js";
import { eInstantGamingTaskType } from "../utils/types/enums/eInstantGamingTaskType.js";
import { fetchInstantGamingOffer } from "./instantGamingFetcher.js";

async function startInstantGamingWorker() {
  const channel = await rabbitConn.createChannel();
  createOrchestratorListener(
    channel,
    config.instantGamingRequests!,
    config.instantGamingResults!,
    async (msg) => {
      if (!msg) return;

      let task: InstantGamingTask | null = parseTask(msg, TaskKind.InstantGaming, channel) as InstantGamingTask | null; // We know it wont be Steam type
      if (!task) return;

      await clearRedisKeyIfExists(task.redisResultKey);

      try {
        const list = [];
        switch (task.type) {
          case eInstantGamingTaskType.SCRAPE_IDS:
            for (const gameId of (task as InstantGamingScrapeIdsTask).gameIds) {
              // TODO: add ignore existing option
              var offer = await fetchInstantGamingOffer(gameId, task.currency, task.proxy)
              if (offer) list.push(offer);
            }
            break;
          case eInstantGamingTaskType.SCRAPE_RANGE:
            for (let gameId = (task as InstantGamingScrapeRangeTask).startId; gameId <= (task as InstantGamingScrapeRangeTask).endId; gameId++) {
              var offer = await fetchInstantGamingOffer(gameId, task.currency, task.proxy)
              if (offer) list.push(offer);
            }
            break;
          case eInstantGamingTaskType.SCRAPE_UP_TO:
            for (let gameId = ++(config.instantGamingSkipFirst as number); gameId <= (task as any).upToId; gameId++) {
              var offer = await fetchInstantGamingOffer(gameId, task.currency, task.proxy)
              if (offer) list.push(offer);
            }
            break;
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