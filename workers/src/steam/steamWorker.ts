import { rabbitConn } from '../utils/config.js';
import { config, redis } from '../utils/config.js';
import { SteamTask, normalizeSteamTask } from '../utils/types/entities/tasks.js';
import { fetchSteamGame } from './gameFetcher.js';
import logger from '../utils/logger.js';
import { createOrchestratorListener } from '../utils/orchestratorListener.js';
import { parseTask, TaskKind } from '../utils/taskParser.js';
import { GameOffer, isGameOffer } from '../utils/types/entities/gameOffer.js';
import { Game, isGame } from '../utils/types/entities/game.js';
import { HttpStatusError } from '../utils/offerFetcher.js';

async function startSteamWorker() {
  const channel = await rabbitConn.createChannel();
  await channel.prefetch(1);

  await createOrchestratorListener(
    channel,
    config.steamRequests!,
    config.steamResults!,
      async (msg) => {
      if (!msg) return;

      let task: SteamTask | null = parseTask(msg, TaskKind.Steam, channel) as SteamTask | null;
      if (!task) return;

      logger.info(`🚀Starting task ${task.taskId} with ${task.gameIds.length} game IDs.`);
      let callsCounter = 0;

      let games: Game[] = [];
      let offers: GameOffer[] = [];
      for (const id of task.gameIds) {
        let fetchResult = await fetchSteamGame(id, task.updateExistingGames, task.updateExistingDeals);
        await new Promise(res => setTimeout(res, config.steamTagsAndGenresRequestDelayMs));
        callsCounter++;

        if (fetchResult instanceof HttpStatusError) {
          if (fetchResult.status === 429) {
          logger.info(`⌚Rate limited while fetching game ${id}. Waiting for ${config.cooldownMs}ms before retrying...`);
          await new Promise(res => setTimeout(res, config.cooldownMs));
          fetchResult = await fetchSteamGame(id, task.updateExistingGames, task.updateExistingDeals);
          callsCounter++;
          } else {
            logger.error(`💥Error fetching game ${id}: ${fetchResult.status}\n\t${fetchResult.message}`, fetchResult.body ?? '');
            continue;
          }
        }

        if (isGame(fetchResult)) games.push(fetchResult);
        else if (isGameOffer(fetchResult)) offers.push(fetchResult);
        else logger.warn(`⚠️Unexpected result for game ${id}:`, fetchResult);

        if (callsCounter % config.maxRequests === 0) {
          await saveDataToRedis(task.redisResultKey, games, offers);
          games = [];
          offers = [];
          await new Promise(res => setTimeout(res, config.cooldownMs));
        }
      }

      await saveDataToRedis(task.redisResultKey, games, offers);
      games = [];
      offers = [];

      try {
        await channel.sendToQueue(
          config.steamResults!, 
          Buffer.from(JSON.stringify({
            taskId: task.taskId,
            redisResultKey: task.redisResultKey
          })),
          { persistent: true }
        )
        channel.ack(msg);
        logger.info(`✅Task ${task.taskId} done, scraped ${callsCounter} games.`);
      } catch (err) {
        logger.error('❌Error processing task:', err);
        channel.nack(msg, false, true);
      }
    }
  )
}

type ProcessResult = {
  successfulCount: number;
  unsuccessfulIds: number[] | null;
}

async function saveDataToRedis(redisKey: string, games: Game[], offers: GameOffer[]) {
  if (games.length > 0) {
    await redis.rpush(
      `${redisKey}:games`,
      ...games.map(g => JSON.stringify(g))
    );
  }

  if (offers.length > 0) {
    await redis.rpush(
      `${redisKey}:offers`,
      ...offers.map(o => JSON.stringify(o))
    );
  }
}

startSteamWorker().catch(logger.crit);
