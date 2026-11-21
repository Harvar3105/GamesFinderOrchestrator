import { getFirstSteamIdFromMediaSourceIG } from "./instantGaminghHelpers.js";
import { fetchJson, HttpStatusError, parseHtmlToDocument } from "./fetch.js";
import { config } from "./config.js";

export type SteamAndGameIds = {
  steamId: number;
  gameId: string;
}

export default async function getSteamAndGameIdsFromHtml(html: string): Promise<SteamAndGameIds | null> {
  var steamId = getFirstSteamIdFromMediaSourceIG(html);

  if (steamId) return await getGameIdsFromBackend({steamId: steamId});

  var vendorsGameName = getVendorsGameNameFromHtml(html);
  //TODO: Find more solutions to name comparison
  var parsedName = vendorsGameName?.split('-')[0].trim();

  if (!parsedName && !vendorsGameName) return null;
  return await getGameIdsFromBackend({name: parsedName ?? vendorsGameName!});
}

type GetGameIdsParams = | { steamId: number; name?: never } | { name: string; steamId?: never };

// 404 means the game is not stored; other backend errors propagate so the offer can be retried.
async function getGameIdsFromBackend(params: GetGameIdsParams): Promise<SteamAndGameIds | null> {
  const url = new URL(config.backendUrl! + config.backendGetGameIds!);
  if (params.steamId) url.searchParams.append('steamId', params.steamId.toString());
  if (params.name) url.searchParams.append('name', params.name);

  try {
    const data = await fetchJson({url: url.toString(), method: 'GET', timeoutMS: config.backendTimeoutMs});
    if (!data?.id) return null;
    return {steamId: data.steamId, gameId: data.id};
  } catch (err) {
    if (err instanceof HttpStatusError && err.status === 404) return null;
    throw err;
  }
}

export function getVendorsGameNameFromHtml(html: string): string | null {
  var doc = parseHtmlToDocument(html);
  var vendorsGameName = (doc.documentElement.querySelector('.game-title') as Element | null)?.textContent;
  return vendorsGameName ?? null;
}
