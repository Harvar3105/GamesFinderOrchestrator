import { v4 } from "uuid";
import { fetchHTML, fetchJson, parseHtmlToDocument } from "../utils/offerFetcher.js";
import { GameOffer } from "../utils/types/entities/gameOffer.js";
import { eCurrency } from "../utils/types/enums/eCurrency.js";
import { getFirstSteamIdFromMediaSource } from "../utils/helpers.js";
import { config } from "../utils/config.js";
import { eVendor } from "../utils/types/enums/eVendor.js";
import { findNoStockElement, findPriceElement } from "../utils/helpers.js";
import logger from "../utils/logger.js";

export async function fetchInstantGamingOffer(id: number, currency?: eCurrency, proxy?: string): Promise<GameOffer | null> {
  const url = `https://www.instant-gaming.com/${id}-/?currency=${currency?.toString().toUpperCase() || 'EUR'}`;
  const data = await fetchHTML(url, proxy);
  if (!data) return null;

  const steamId = getFirstSteamIdFromMediaSource(data)
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
  var priceElement = findPriceElement(doc);

  var gameOffer: GameOffer = {
    id: offerId,
    createdAt: new Date().toUTCString(),
    updatedAt: new Date().toUTCString(),
    gameId: gameId,
    vendorsGameId: id.toString(),
    vendor: eVendor.InstatnGaming,
    vendorsUrl: url,
    available: findNoStockElement(doc) ? false : true,
    amount: priceElement?.price ?? null,
    currency: priceElement?.currency ?? null
  }

  return gameOffer;
}

async function getGameId(steamId: number): Promise<string | null> {
  const url = config.backendUrl! + config.backendCheckGame! + `?steamId=${steamId}`;
  const data = await fetchJson(url, undefined, 'POST');
  if (data && data.id) return data.id;
  return null;
}