import amqp from 'amqplib';
import { Redis } from 'ioredis';
import path from 'path';
import { fileURLToPath } from 'url';

if (process.env.NODE_ENV !== 'docker') {
  import('dotenv').then(dotenv => {
    const __filename = fileURLToPath(import.meta.url);
    const __dirname = path.dirname(__filename);
    
    dotenv.config({
      path: path.resolve(__dirname, '../.env')
    })
  });
}


export const config = {
  rabbitUrl: process.env.RABBIT_URL || 'amqp://localhost',
  defaultQueue: process.env.RabbitMQ__DefaultQueue,

  steamRequests: process.env.RabbitMQ__SteamRequestsQueue,
  steamResults: process.env.RabbitMQ__SteamResultsQueue,
  maxRequests: Number(process.env.MAX_REQUESTS) || 200,
  cooldownMs: Number(process.env.COOLDOWN_MS) || 5 * 60 * 1000,

  instantGamingRequests: process.env.RabbitMQ__InstantGamingRequestsQueue,
  instantGamingResults: process.env.RabbitMQ__InstantGamingResultsQueue,
  instantGamingSkipFirst: process.env.INSTANT_GAMING_SKIP_FIRST || 10,

  backendUrl: process.env.BACKEND_URL,
  backendCheckGame: process.env.BACKEND_CHECK_GAME,
  backendTimeoutMs: Number(process.env.BACKEND_TIMEOUT_MS) || 5000,
};

export const redis = new Redis({
  host: process.env.Redis__Host,
  port: Number(process.env.Redis__Port),
  password: process.env.Redis__Password
});

export const rabbitConn = await amqp.connect({
  hostname: process.env.RabbitMQ__HostName,
  port: Number(process.env.RabbitMQ__Port),
  username: process.env.RabbitMQ__Username,
  password: process.env.RabbitMQ__Password
});