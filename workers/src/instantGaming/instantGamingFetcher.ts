import { v4 } from "uuid";
import { fetchHTML, fetchJson, HttpStatusError, parseHtmlToDocument } from "../utils/offerFetcher.js";
import { GameOffer } from "../utils/types/entities/gameOffer.js";
import { eCurrency } from "../utils/types/enums/eCurrency.js";
import { getCanonicalIGurl, getFirstSteamIdFromMediaSourceIG } from "../utils/instantGaminghHelpers.js";
import { config } from "../utils/config.js";
import { eVendor } from "../utils/types/enums/eVendor.js";
import { findNoStockElementIG, findPriceElementIG } from "../utils/instantGaminghHelpers.js";
import logger from "../utils/logger.js";

export async function fetchInstantGamingOffer(id: number, currency?: eCurrency, proxy?: string): Promise<GameOffer | null | HttpStatusError> {
  const url = `https://www.instant-gaming.com/${id}-/?currency=${currency?.toString().toUpperCase() || 'EUR'}`;
  
  let data;
  try {
    data = await fetchJson(url);
  } catch (err) {
    if (err instanceof HttpStatusError) return err;
    return null;
  }

  const steamId = getFirstSteamIdFromMediaSourceIG(data)
  if (!steamId) {
    logger.error(`Could not recognize game from ${url}`);
    return null;
  }

  const gameId = await getGameId(steamId);
  if (!gameId){
    logger.error(`Could not find gameId for steamId ${steamId} from ${url}`);
    return null;
  }

  const offerId = v4();

  const doc = parseHtmlToDocument(data);
  var priceElement = findPriceElementIG(doc);
  const canonicalUrl = getCanonicalIGurl(doc) ?? url;

  var gameOffer: GameOffer = {
    id: offerId,
    createdAt: new Date().toUTCString(),
    updatedAt: new Date().toUTCString(),
    gameId: gameId,
    vendorsGameId: id.toString(),
    vendor: eVendor.InstatnGaming,
    vendorsUrl: canonicalUrl,
    available: findNoStockElementIG(doc) ? false : true,
    amount: priceElement?.price ?? null,
    currency: priceElement?.currency ?? null
  }

  return gameOffer;
}

async function getGameId(steamId: number): Promise<string | null> {
  const url = config.backendUrl! + config.backendCheckGame! + `?steamId=${steamId}&getId=true`;
  const data = await fetchJson(url, undefined, 'POST');
  if (!data || !data.exists) return null;
  return data.gameId;
}