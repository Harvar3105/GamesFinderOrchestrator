import { getFirstSteamIdFromMediaSourceIG } from "./instantGaminghHelpers.js";
import { fetchJson, parseHtmlToDocument } from "./offerFetcher.js";
import { config } from "./config.js";
import logger from "./logger.js";

export type SteamAndGameIds = {
  steamId: number;
  gameId: string;
}

export default async function getSteamAndGameIdsFromHtml(html: string): Promise<SteamAndGameIds | null> {
  var steamId = getFirstSteamIdFromMediaSourceIG(html);

  if (steamId) {
    var gameId = await getGameId(steamId);
    return {steamId: steamId, gameId: gameId!};
  } else {
    var vendorsGameName = getVendorsGameNameFromHtml(html);
    //TODO: Find more solutions to name comparison
    var parsedName = vendorsGameName?.split('-')[0].trim();
    
    if (!parsedName && !vendorsGameName) return null;
    return await getIdsFromBackend(parsedName ?? vendorsGameName!);
  }
}

async function getIdsFromBackend(gameName: string): Promise<SteamAndGameIds | null> {
  const url = config.backendUrl! + config.backendCheckGameExistsByName + `?gameName=${encodeURIComponent(gameName)}&getSteamId=true`;
  const data = await fetchJson({url: url, method: 'GET', timeoutMS: config.backendTimeoutMs});
  if (!data?.exists) return null;
  return {steamId: data.steamId, gameId: data.gameId};
}

async function getGameId(steamId: number): Promise<string | null> {
  const url = config.backendUrl! + config.backendGetGameId! + `?steamId=${encodeURIComponent(steamId)}`;
  try {
    const data = await fetchJson({url: url, method: 'GET', timeoutMS: config.backendTimeoutMs});
    if (!data || !data.id) return null;
    return data.id;
  } catch (err) {
    logger.error("Could not get gameId.", err);
  }
  return null;
}

export function getVendorsGameNameFromHtml(html: string): string | null {
  var doc = parseHtmlToDocument(html);
  var vendorsGameName = (doc.documentElement.querySelector('.game-title') as Element | null)?.innerHTML;
  return vendorsGameName ?? null;
}
