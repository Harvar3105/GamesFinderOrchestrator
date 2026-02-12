import { config } from "./utils/config.js";
import logger from "./utils/logger.js";
import { Game } from "./utils/types/entities/game.js";

export async function checkGameExists(gameId: number, getGame: boolean = false): Promise<boolean | null | Game> {
  const url = new URL(config.backendUrl! + config.backendCheckGame!);
  url.searchParams.append('steamId', gameId.toString());
  url.searchParams.append('getGame', getGame.toString());

  const response = await processBackendRequest(url);
  if (!response) return null;

  try {
    const data = await response.json();
    if (getGame) {
      return data.Game as Game;
    } else {
      return data.Exists as boolean;
    }
  } catch (err) {
    logger.error(`💥Error parsing JSON response when checking game existence for Steam ID ${gameId}:`, err);
    return null;
  }
}

export async function checkSteamOfferExists(steamId: number): Promise<boolean | null> {
  const url = new URL(config.backendUrl! + config.backendCheckSteamOffer!);
  url.searchParams.append('steamId', steamId.toString());

  const response = await processBackendRequest(url);
  if (!response) return null;

  try {
    const data = await response.json();
    return data.Exists as boolean;
  } catch (err) {
    logger.error(`💥Error parsing JSON response when checking Steam offer existence for Steam ID ${steamId}:`, err);
    return null;
  } 
}

export async function getGameIdBySteamIdAsync(steamId: number): Promise<string | null> {
  const url = new URL(config.backendUrl! + config.backendGetGameId!);
  url.searchParams.append('steamId', steamId.toString());

  const response = await processBackendRequest(url);
  if (!response) return null;

  try {
    const data = await response.json();
    return data.id as string;
  } catch (err) {
    logger.error(`💥Error parsing JSON response when checking Steam offer existence for Steam ID ${steamId}:`, err);
    return null;
  }  
}

async function processBackendRequest(url: URL): Promise<Response | null>{
  const response = await fetch(url.toString(), {
    method: 'POST',
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