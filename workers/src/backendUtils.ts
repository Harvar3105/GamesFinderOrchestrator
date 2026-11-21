import { config } from "./utils/config.js";
import logger from "./utils/logger.js";
import { eVendor } from "./utils/types/enums/eVendor.js";

export type CheckGameExistsParams =
  | { id: string; steamId?: never; name?: never }
  | { steamId: number; id?: never; name?: never }
  | { name: string; id?: never; steamId?: never };

// HEAD <backendCheckGame>: 200 — exists, 404 — not found. Any other outcome is logged and treated as "not found".
export async function checkGameExists(params: CheckGameExistsParams): Promise<boolean> {
  const url = new URL(config.backendUrl! + config.backendCheckGame!);
  for (const [key, value] of Object.entries(params)) url.searchParams.append(key, String(value));

  try {
    const response = await fetch(url.toString(), {
      method: 'HEAD',
      signal: AbortSignal.timeout(config.backendTimeoutMs),
    });
    if (response.ok) return true;
    if (response.status !== 404) logger.error(`💥Error checking game existence (${url.search}): ${response.status} ${response.statusText}`);
  } catch (err) {
    logger.error(`💥Error checking game existence (${url.search}):`, err);
  }
  return false;
}

// HEAD <backendCheckGameOffer>: 200 — an offer of the vendor exists, 404 — it does not.
// The offer is looked up either by our game id or by the id of the offer in the vendor system.
// Returns null when the backend could not answer, so the caller can decide whether to retry.
export async function checkGameOfferExists({gameId, vendorId}: GetOfferRequestParams, vendor: eVendor): Promise<boolean | null> {
  const url = new URL(config.backendUrl! + config.backendCheckGameOffer!);
  if (gameId) url.searchParams.append('gameId', gameId);
  if (vendorId) url.searchParams.append('vendorId', vendorId);
  url.searchParams.append('vendor', vendor);

  try {
    const response = await fetch(url.toString(), {
      method: 'HEAD',
      signal: AbortSignal.timeout(config.backendTimeoutMs),
    });
    if (response.ok) return true;
    if (response.status === 404) return false;
    logger.error(`💥Error checking game offer existence (${url.search}): ${response.status} ${response.statusText}`);
  } catch (err) {
    logger.error(`💥Error checking game offer existence (${url.search}):`, err);
  }
  return null;
}

export async function getGameIdBySteamIdAsync(steamId: number): Promise<string | null> {
  const url = new URL(config.backendUrl! + config.backendGetGameIds!);
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

// GET <backendGetOfferId>: { id } of the vendor's offer, looked up either by our game id
// or by the id of the offer in the vendor system. Returns null when not found or on error.
export async function getOfferId({gameId, vendorId}: GetOfferRequestParams, vendor: eVendor): Promise<string | null> {
  const url = new URL(config.backendUrl! + config.backendGetOfferId!);
  if (gameId) url.searchParams.append('gameId', gameId);
  if (vendorId) url.searchParams.append('vendorId', vendorId);
  url.searchParams.append('vendor', vendor);

  const response = await processBackendRequest(url, 'GET');
  if (!response) return null;

  try {
    const data = await response.json();
    return data.id as string;
  } catch (err) {
    logger.error(`💥Error parsing JSON response when getting offer ID (${url.search}):`, err);
    return null;
  }
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