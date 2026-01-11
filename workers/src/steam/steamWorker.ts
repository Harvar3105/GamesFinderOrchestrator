import { rabbitConn } from '../utils/config.js';
import { config, redis } from '../utils/config.js';
import { SteamTask, normalizeSteamTask } from '../utils/types/entities/tasks.js';
import { scrapeBatch } from './gameFetcher.js';
import { Game } from '../utils/types/entities/game.js';
import logger from '../utils/logger.js';
import { splitIntoBatches } from '../utils/helpers.js';
import { clearRedisKeyIfExists, createOrchestratorListener } from '../utils/orchestratorListener.js';
import { parseTask, TaskKind } from '../utils/taskParser.js';

async function startSteamWorker() {
  const channel = await rabbitConn.createChannel();

  createOrchestratorListener(
    channel,
    config.steamRequests!,
    config.steamResults!,
      async (msg) => {
      if (!msg) return;

      let task: SteamTask | null = parseTask(msg, TaskKind.Steam, channel);
      if (!task) return;

      await clearRedisKeyIfExists(task.redisResultKey);
      
      var batches = splitIntoBatches(task.gameIds, config.maxRequests);

      try {
        let scrapedCount = 0;
        for (const batch of batches) {
          const result: Game[] = await scrapeBatch(batch);
          
          if (result.length > 0) {
            scrapedCount += result.length;

            await redis.rpush(task.redisResultKey, ...result.map(r => JSON.stringify(r)));
            logger.info(`ℹ️Just added ${result.length} games to redis, total so far: ${scrapedCount}`);
          }

          if (batch.length === 200) await new Promise(res => setTimeout(res, config.cooldownMs));
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
        logger.info(`✅Task ${task.taskId} done, scraped ${scrapedCount} games.`);
      } catch (err) {
        logger.error('❌Error processing task:', err);
        channel.nack(msg, false, true);
      }
    }
  )
}

startSteamWorker().catch(logger.crit);
