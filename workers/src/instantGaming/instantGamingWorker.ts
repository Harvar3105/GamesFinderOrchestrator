import { rabbitConn } from "../utils/config.js";
import { config, redis } from "../utils/config.js";
import logger from "../utils/logger.js";
import { createOrchestratorListener } from "../utils/orchestratorListener.js";
import { parseTask, TaskKind } from "../utils/taskParser.js";
import { InstantGamingTask } from "../utils/types/entities/tasks.js";
import processInstantGamingTask from "./processInstantGamingTask.js";

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

      const data = await processInstantGamingTask(task, msg);

      if (data?.length === 0) {
        logger.info(`⚠️No offers found for task ${task.taskId}, acknowledging without adding to redis.`);
        channel.ack(msg);
        return;
      }

      await redis.rpush(task.redisResultKey, ...data.map(r => JSON.stringify(r)));
      logger.info(`ℹ️Just added ${data.length} gameOffers to redis from InstantGaming.}`);

      await channel.sendToQueue(
        config.instantGamingResults!,
        Buffer.from(JSON.stringify({
          taskId: task.taskId,
          redisResultKey: task.redisResultKey,
        })),
        { persistent: true }
      );
    
      channel.ack(msg);
      logger.info(`✅InstantGaming task ${task.taskId} done, created ${data.length} offers.`);
    }
  )
}

startInstantGamingWorker().catch(logger.crit);