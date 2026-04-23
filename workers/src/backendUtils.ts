import { config } from "./utils/config.js";
import logger from "./utils/logger.js";
import { Game } from "./utils/types/entities/game.js";

export async function checkGameExists(gameId: number, getGame: boolean = false): Promise<boolean | null | Game> {
  const url = new URL(config.backendUrl! + config.backendCheckGame!);
  url.searchParams.append('steamId', gameId.toString());
  url.searchParams.append('getGame', getGame.toString());

  const response = await processBackendRequest(url, 'GET');
  if (!response) return null;

  try {
    const data = await response.json();
    logger.info(`Parsed response data: ${JSON.stringify(data)}`);
    if (getGame) {
      return data.game as Game;
    } else {
      return data.exists as boolean;
    }
  } catch (err) {
    logger.error(`💥Error parsing JSON response when checking game existence for Steam ID ${gameId}:`, err);
    return null;
  }
}

export async function checkSteamOfferExists(steamId: number): Promise<boolean | null> {
  const url = new URL(config.backendUrl! + config.backendCheckSteamOffer!);
  url.searchParams.append('steamId', steamId.toString());

  const response = await processBackendRequest(url, 'GET');
  if (!response) return null;

  try {
    const data = await response.json();
    return data.exists as boolean;
  } catch (err) {
    logger.error(`💥Error parsing JSON response when checking Steam offer existence for Steam ID ${steamId}:`, err);
    return null;
  } 
}

export async function checkIgOfferExists(vendorId: string): Promise<boolean | null> {
  const url = new URL(config.backendUrl! + config.backendCheckIgOffer!);
  url.searchParams.append('vendorId', vendorId);

  const response = await processBackendRequest(url, 'GET');
  if (!response) return null;

  try {
    const data = await response.json();
    return data.exists as boolean;
  } catch (err) {
    logger.error(`💥Error parsing JSON response when checking Steam offer existence for Steam ID ${vendorId}:`, err);
    return null;
  } 
}

export async function getGameIdBySteamIdAsync(steamId: number): Promise<string | null> {
  const url = new URL(config.backendUrl! + config.backendGetGameId!);
  url.searchParams.append('steamId', steamId.toString());

  const response = await processBackendRequest(url, 'GET');
  if (!response) return null;

  try {
    const data = await response.json();
    return data.id as string;
  } catch (err) {
    logger.error(`💥Error parsing JSON response when checking Steam offer existence for Steam ID ${steamId}:`, err);
    return null;
  }  
}

export type GetOfferRequestParams = | { gameId: string; vendorId?: never } | { vendorId: string; gameId?: never }

export async function getIgOfferId({gameId, vendorId}: GetOfferRequestParams): Promise<string | null> {
  if ((!gameId && !vendorId) || (gameId && vendorId)) {
    logger.error(`💥Invalid parameters for getIgOfferId: gameId and vendorId must be provided together.`);
    return null;
  }

  const url = new URL(config.backendUrl! + config.backendGetIgOfferId!);
  if (gameId) url.searchParams.append('gameId', gameId);
  if (vendorId) url.searchParams.append('vendorId', vendorId);

  const response = await processBackendRequest(url, 'GET');
  if (!response) return null;

  try {
    const data = await response.json();
    return data.offerId as string;
  } catch (err) {
    logger.error(`💥Error parsing JSON response when getting Instant Gaming offer ID for gameId ${gameId} and vendorId ${vendorId}:`, err)
    return null
  };
}

export async function getSteamOfferId({gameId, vendorId}: GetOfferRequestParams): Promise<string | null> {
  if ((!gameId && !vendorId) || (gameId && vendorId)) {
    logger.error(`💥Invalid parameters for getIgOfferId: gameId and steamId must be provided together.`);
    return null;
  }
  if (vendorId && isNaN(Number(vendorId))){
    logger.error(`💥Invalid vendorId for getSteamOfferId: ${vendorId} is not a valid number.`)
    return null;
  }

  const url = new URL(config.backendUrl! + config.backendGetSteamOfferId!);
  if (gameId) url.searchParams.append('gameId', gameId);
  if (vendorId) url.searchParams.append('steamId', vendorId);

  const response = await processBackendRequest(url, 'GET');
  if (!response) return null;

  try {
    const data = await response.json();
    return data.offerId as string;
  } catch (err) {
    logger.error(`💥Error parsing JSON response when getting Instant Gaming offer ID for gameId ${gameId} and vendorId ${vendorId}:`, err)
    return null
  };
}

async function processBackendRequest(url: URL, method: string): Promise<Response | null>{
  const response = await fetch(url.toString(), {
    method: method,
    headers: {
      'Content-Type': 'application/json'
    },
    signal: AbortSignal.timeout(config.backendTimeoutMs)
  });

  if (!response.ok) {
    const errorText = await response.text().catch(() => 'No response body');
    logger.error(`💥Error checking Steam offer existence for Steam ID ${url.searchParams.get('steamId')}: ${response.status} ${response.statusText}. Response body: ${errorText}`);
    return null;
  }
  return response;
}