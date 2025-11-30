import { v4 } from "uuid";
import { fetchJson } from "../utils/offerFetcher.js";
import { GameOffer } from "../utils/types/entities/gameOffer.js";
import { eCurrency } from "../utils/types/enums/eCurrency.js";
import { getFirstSteamIdFromMediaSource } from "../utils/helpers.js";
import { config } from "../utils/config.js";
import { eVendor } from "../utils/types/enums/eVendor.js";
import { findNoStockElement, findPriceElement } from "../utils/helpers.js";

export async function fetchInstantGamingOffer(id: number, currency?: eCurrency, proxy?: string): Promise<GameOffer | null> {
  const url = `https://www.instant-gaming.com/${id}-/?currency=${currency?.toString().toUpperCase() || 'EUR'}`;
  const data = await fetchJson(url, proxy);

  const steamId = getFirstSteamIdFromMediaSource(data.toString());
  if (!data || !steamId) return null;

  const gameId = await getGameId(steamId);
  if (!gameId) return null;

  const offerId = v4();  

  var gameOffer: GameOffer = {
    id: offerId,
    createdAt: new Date().toUTCString(),
    updatedAt: new Date().toUTCString(),
    gameId: gameId,
    vendorsGameId: id.toString(),
    vendor: eVendor.InstatnGaming,
    vendorsUrl: url,
    available: findNoStockElement(data) ? false : true,
    price: findPriceElement(data)
  }

  return gameOffer;
}

async function getGameId(steamId: number): Promise<string | null> {
  const url = config.backendUrl! + config.backendCheckGame! + `?steamID=${steamId}`;
  const data = await fetchJson(url);
  if (data && data.id) return data.id;
  return null;
}