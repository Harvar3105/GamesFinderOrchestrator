import { vi } from "vitest";

export const config = {
  rabbitUrl: "amqp://test",
  defaultQueue: "default",

  steamRequests: "steam-requests",
  steamResults: "steam-results",
  steamTagsAndGenresRequestDelayMs: 0,
  steamTagsAndGenresTimeoutMS: 1000,
  steamApiTimeoutMs: 1000,
  maxRequests: 200,
  cooldownMs: 5 * 60 * 1000,

  instantGamingRequests: "instantGaming-requests",
  instantGamingResults: "instantGaming-results",

  backendUrl: "http://backend.test",
  backendCheckGame: "/api/Steam/checkGameExists",
  backendCheckGameExistsByName: "/api/Steam/checkGameExistsByName",
  backendCheckGameExistsBySteamId: "/api/Steam/checkGameExistsBySteamId",
  backendCheckSteamOffer: "/api/Steam/checkGameOfferExists",
  backendCheckIgOffer: "/api/InstantGaming/checkGameOfferExists",
  backendGetIgOfferId: "/api/InstantGaming/getOfferId",
  backendGetSteamOfferId: "/api/Steam/getOfferId",
  backendGetGameId: "/api/Steam/getGameIdBySteamId",
  backendTimeoutMs: 1000,
};

export const testChannel = {
  prefetch: vi.fn(async () => undefined),
  ack: vi.fn(),
  nack: vi.fn(),
  sendToQueue: vi.fn(() => true),
  assertQueue: vi.fn(async () => undefined),
  consume: vi.fn(async () => undefined),
};

export const redis = {
  rpush: vi.fn(async () => 1),
  exists: vi.fn(async () => 0),
  del: vi.fn(async () => 1),
};

export const rabbitConn = {
  createChannel: vi.fn(async () => testChannel),
};
