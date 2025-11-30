import { rabbitConn } from '../utils/config.js';
import { config, redis } from '../utils/config.js';
import { SteamTask, normalizeSteamTask } from '../utils/types/entities/tasks.js';
import { scrapeBatch } from './gameFetcher.js';
import { Game } from '../utils/types/entities/game.js';
import logger from '../utils/logger.js';
import { splitIntoBatches } from '../utils/helpers.js';

async function startSteamWorker() {
  const channel = await rabbitConn.createChannel();

  await channel.assertQueue(config.steamRequests!, { durable: true });
  logger.info(`✅Steam worker listening on queue "${config.steamRequests}"...`);
  await channel.assertQueue(config.steamResults!, { durable: true });
  logger.info(`✅Steam worker ready to answear on queue "${config.steamResults}"...`)

  await channel.consume(config.steamRequests!, async (msg) => {
    if (!msg) return;

    let task: SteamTask;
    try {
      task = normalizeSteamTask(JSON.parse(msg.content.toString()));
    } catch (err) {
      logger.error('❌Invalid JSON from queue:', msg.content.toString());
      channel.nack(msg, false, false);
      return;
    }


    if (await redis.exists(task.redisResultKey)) {
      await redis.del(task.redisResultKey);
      logger.warn(`⚠️Cleared existing Redis key from previous request: ${task.redisResultKey}`);
    }
    
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
  });
}

startSteamWorker().catch(logger.crit);
