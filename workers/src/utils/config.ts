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
  defaultQueue: process.env.RABBITMQ_DEFAULT_QUEUE,

  steamRequests: process.env.RABBITMQ_STEAM_REQUESTS_QUEUE,
  steamResults: process.env.RABBITMQ_STEAM_RESULTS_QUEUE,
  maxRequests: Number(process.env.MAX_REQUESTS) || 200,
  cooldownMs: Number(process.env.COOLDOWN_MS) || 5 * 60 * 1000,

  instantGamingRequests: process.env.RABBITMQ_IG_REQUESTS_QUEUE,
  instantGamingResults: process.env.RABBITMQ_IG_RESULTS_QUEUE,
  instantGamingSkipFirst: process.env.INSTANT_GAMING_SKIP_FIRST || 10,

  backendUrl: process.env.BACKEND_URL,
  backendCheckGame: process.env.BACKEND_CHECK_GAME,
  backendTimeoutMs: Number(process.env.BACKEND_TIMEOUT_MS) || 5000,
};

export const redis = new Redis({
  host: process.env.REDIS_HOST,
  port: Number(process.env.REDIS_PORT),
  password: process.env.REDIS_PASSWORD
});

export const rabbitConn = await amqp.connect({
  hostname: process.env.RABBITMQ_HOST,
  port: Number(process.env.RABBITMQ_PORT),
  username: process.env.RABBITMQ_USERNAME,
  password: process.env.RABBITMQ_PASSWORD
});