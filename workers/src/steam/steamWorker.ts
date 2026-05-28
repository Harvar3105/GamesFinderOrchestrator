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

  createOrchestratorListener(
    channel,
    config.steamRequests!,
    config.steamResults!,
      async (msg) => {
      if (!msg) return;

      let task: SteamTask | null = parseTask(msg, TaskKind.Steam, channel) as SteamTask | null;
      if (!task) return;

      logger.info(`🚀Starting task ${task.taskId} with ${task.gameIds.length} game IDs.`);
      let counter = 0;
      
      const firstTry = await processIds(task.gameIds, task.updateExistingGames, task.updateExistingDeals, task.taskId, task.redisResultKey);
      counter += firstTry.successfulCount;
      
      if (firstTry.unsuccessfulIds && firstTry.unsuccessfulIds.length > 0) {
        logger.warn(`⚠️Retrying ${firstTry.unsuccessfulIds.length} unsuccessful IDs for task ${task.taskId} after first attempt...`);
        await new Promise(res => setTimeout(res, config.cooldownMs));
        const retryResult = await processIds(firstTry.unsuccessfulIds, task.updateExistingGames, task.updateExistingDeals, task.taskId, task.redisResultKey);
        counter += retryResult.successfulCount;
        logger.info(`✅Task ${task.taskId} done after retry, scraped additional ${retryResult.successfulCount} games.`);
      }


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
        logger.info(`✅Task ${task.taskId} done, scraped ${counter} games.`);
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

async function processIds(gameIds: number[], updateGames: boolean, updateDeals: boolean, taskId: string, taskRedisKey: string) : Promise<ProcessResult> {
  let games: Game[] = [];
  let offers: GameOffer[] = [];
  let unseccessfulIds: number[] = [];
  let counter = 0;
  for (const id of gameIds) {
    const fetchResult = await fetchSteamGame(id, updateGames, updateDeals);
    await new Promise(res => setTimeout(res, config.steamTagsAndGenresRequestDelayMs));
    
    if (fetchResult instanceof HttpStatusError) {
      logger.error(`💥Error fetching game ${id}: ${fetchResult.status}\n\t${fetchResult.message}`, fetchResult.body ?? '');
      unseccessfulIds.push(id);
      continue;
    }

    if (isGame(fetchResult)) games.push(fetchResult);
    else if (isGameOffer(fetchResult)) offers.push(fetchResult);
    else logger.warn(`⚠️Unexpected result for game ${id}:`, fetchResult);

    counter++;

    if (counter % 200 === 0){
      logger.info(`⌚Scraped ${counter} games so far for task ${taskId}...`);
      await saveDataToRedis(taskRedisKey, games, offers);
      games = [];
      offers = [];
      await new Promise(res => setTimeout(res, config.cooldownMs));
    }
  }
  await saveDataToRedis(taskRedisKey, games, offers);
  games = [];
  offers = [];

  let result: ProcessResult = {successfulCount: counter, unsuccessfulIds: null};
  if (unseccessfulIds.length > 0) {
    result.successfulCount = counter - unseccessfulIds.length;
    result.unsuccessfulIds = unseccessfulIds;
  }
  return result;
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
