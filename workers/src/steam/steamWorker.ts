import { rabbitConn } from '../utils/config.js';
import { config, redis } from '../utils/config.js';
import { SteamTask } from '../utils/types/entities/tasks.js';
import logger from '../utils/logger.js';
import { createOrchestratorListener } from '../utils/orchestratorListener.js';
import { parseTask, TaskKind } from '../utils/taskParser.js';
import { GameOffer } from '../utils/types/entities/gameOffer.js';
import { Game } from '../utils/types/entities/game.js';
import processSteamWorkerTask, { processSteamWorkerIds } from './processSteamWorkerTask.js';

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

      var batches = [];
      for (let i = 0; i < task.gameIds.length; i += config.maxRequests) {
        batches.push(task.gameIds.slice(i, i + config.maxRequests));
      }

      for (const batch of batches) {
        var data = await processSteamWorkerTask(batch, task.taskId, task.updateExistingGames, task.updateExistingDeals);
        await saveDataToRedis(task.redisResultKey, data.games, data.offers);
        logger.info(`ℹ️Just added ${data.games.length} games and ${data.offers.length} offers to redis from Steam.}`);
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
      logger.info(`✅Task ${task.taskId} done, scraped ${task.gameIds.length} games.`);
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
