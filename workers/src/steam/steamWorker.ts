import { rabbitConn } from '../utils/config.js';
import { config, redis } from '../utils/config.js';
import { SteamTask, normalizeSteamTask } from '../utils/types/entities/tasks.js';
import { scrapeBatch, scrapeResult } from './gameFetcher.js';
import { Game } from '../utils/types/entities/game.js';
import logger from '../utils/logger.js';
import { splitIntoBatches } from '../utils/instantGaminghHelpers.js';
import { clearRedisKeyIfExists, clearRedisSteamKeys, createOrchestratorListener } from '../utils/orchestratorListener.js';
import { parseTask, TaskKind } from '../utils/taskParser.js';
import { HttpStatusError } from '../utils/offerFetcher.js';
import { GameOffer } from '../utils/types/entities/gameOffer.js';
import { ChainableCommander } from 'ioredis';

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

      await clearRedisSteamKeys(task.redisResultKey);
      
      // TODO: Consider scraping not in batches but one by one with a cooldown after 429 response
      var queue;
      task.gameIds.length > config.maxRequests 
        ? queue = splitIntoBatches(task.gameIds, config.maxRequests)
        : queue = [task.gameIds];

      logger.info(`🚀Starting task ${task.taskId} with ${task.gameIds.length} game IDs, split into ${queue.length} batches.`);

      try {
        let total = 0;
        while (queue.length > 0) {
          const batch = queue.shift()!;
          const multi = redis.multi();
          logger.info(`Processing batch of ${batch.length} game IDs for task ${task.taskId}...`);
          const result: scrapeResult = await scrapeBatch(batch, task.updateExistingGames, task.updateExistingDeals);
          total += batch.length;

          if (result.err && result.unprocessedIds) {
            logger.error(`Stopping batch processing due to error: HTTP ${result.err.status} ${result.err.message}`, result.err.body ?? '');
            total -= result.unprocessedIds.length;
            await saveDataToRedis(task.redisResultKey, result, multi);
            queue.push(result.unprocessedIds);
            await new Promise(res => setTimeout(res, config.cooldownMs));
            continue;
          }

          await saveDataToRedis(task.redisResultKey, result, multi);
          logger.info(`ℹ️Just added ${result.games.length} games to redis, scraped ${total} ids so far.`);

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

async function saveDataToRedis(redisKey: string, result: scrapeResult, multi: ChainableCommander) {
  if (result.games.length > 0) {
    multi.rpush(
      `${redisKey}:games`,
      ...result.games.map(g => JSON.stringify(g))
    );
  }

  if (result.offers.length > 0) {
    multi.rpush(
      `${redisKey}:offers`,
      ...result.offers.map(o => JSON.stringify(o))
    );
  }
  await multi.exec();
}

startSteamWorker().catch(logger.crit);
