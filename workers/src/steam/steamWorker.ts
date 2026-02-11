import { rabbitConn } from '../utils/config.js';
import { config, redis } from '../utils/config.js';
import { SteamTask, normalizeSteamTask } from '../utils/types/entities/tasks.js';
import { scrapeBatch, scrapeResult } from './gameFetcher.js';
import { Game } from '../utils/types/entities/game.js';
import logger from '../utils/logger.js';
import { splitIntoBatches } from '../utils/instantGaminghHelpers.js';
import { clearRedisKeyIfExists, createOrchestratorListener } from '../utils/orchestratorListener.js';
import { parseTask, TaskKind } from '../utils/taskParser.js';
import { HttpStatusError } from '../utils/offerFetcher.js';

async function startSteamWorker() {
  const channel = await rabbitConn.createChannel();

  createOrchestratorListener(
    channel,
    config.steamRequests!,
    config.steamResults!,
      async (msg) => {
      if (!msg) return;

      let task: SteamTask | null = parseTask(msg, TaskKind.Steam, channel) as SteamTask | null;
      if (!task) return;

      await clearRedisKeyIfExists(task.redisResultKey);
      
      var batches;
      task.gameIds.length > config.maxRequests 
        ? batches = splitIntoBatches(task.gameIds, config.maxRequests)
        : batches = [task.gameIds];

      logger.info(`🚀Starting task ${task.taskId} with ${task.gameIds.length} game IDs, split into ${batches.length} batches.`);

      try {
        let total = 0;
        for (const batch of batches) {
          logger.info(`Processing batch of ${batch.length} game IDs for task ${task.taskId}...`);
          const result: scrapeResult = await scrapeBatch(batch);

          if (result.err && result.unprocessedIds) {
            logger.error(`Stopping batch processing due to error: HTTP ${result.err.status} ${result.err.message}`, result.err.body ?? '');
            batches.push(result.unprocessedIds);
            await new Promise(res => setTimeout(res, config.cooldownMs));
            continue;
          }
          
          if (result.res.length > 0) {
            total += result.res.length;

            await redis.rpush(task.redisResultKey, ...result.res.map(r => JSON.stringify(r)));
            logger.info(`ℹ️Just added ${result.res.length} games to redis, total so far: ${total}`);
          }

          if (total % config.maxRequests === 0) await new Promise(res => setTimeout(res, config.cooldownMs));
        }

        await channel.sendToQueue(
          config.steamResults!, 
          Buffer.from(JSON.stringify({
            taskId: task.taskId,
            redisResultKey: task.redisResultKey
          })),
          { persistent: true }
        )
        channel.ack(msg);
        logger.info(`✅Task ${task.taskId} done, scraped ${total} games.`);
      } catch (err) {
        logger.error('❌Error processing task:', err);
        channel.nack(msg, false, true);
      }
    }
  )
}

startSteamWorker().catch(logger.crit);
