import { v4 } from "uuid";
import { Game } from "../utils/types/entities/game.js";
import { eCurrency, getECurrencyFromString } from "../utils/types/enums/eCurrency.js";
import { eRegion } from "../utils/types/enums/eRegion.js";
import { eVendor } from "../utils/types/enums/eVendor.js";
import { GameOffer } from "../utils/types/entities/gameOffer.js";
import logger from "../utils/logger.js";
import { fetchJson, HttpStatusError } from "../utils/offerFetcher.js";

export async function fetchSteamGame(id: number, region: eRegion = eRegion.US ): Promise<Game | null | HttpStatusError> {
  const url = `https://store.steampowered.com/api/appdetails?appids=${id}&cc=${region}&l=en`;
  
  let data;
  try {
    data = await fetchJson(url);
  } catch (err) {
    if (err instanceof HttpStatusError) return err;
    return null;
  }
  

  if (!data[id]?.success) return null;
  const game = data[id].data;

  const gameId = v4();
  const isReleased = !game.release_date.coming_soon;
  let offers = null;

  const vendorsUrl = `https://store.steampowered.com/app/${id}`

  let currency: eCurrency | null = null;
  let initialAmount: number | null = null;

  if (isReleased){
    try {
      // Might depend on region but due to steam api poor typization we do it like this
      currency = getECurrencyFromString(game.price_overview?.currency || 'null')!; 
      initialAmount = Number((game.price_overview?.initial / 100).toFixed(2));

      const currentAmount = Number((game.price_overview?.final / 100).toFixed(2));
      offers = [{
        id: v4(),
        createdAt: new Date().toUTCString(),
        updatedAt: new Date().toUTCString(),
        gameId: gameId,
        vendorsGameId: id.toString(),
        vendor: eVendor.Steam,
        vendorsUrl: vendorsUrl,
        available: true,
        amount: currentAmount,
        currency: currency
      } as GameOffer]
    } catch (e) {
      logger.error(`❌Error parsing price for game ID ${id}:`, e);
    }
  }

  return {
    id: gameId,
    createdAt: new Date().toUTCString(),
    updatedAt: new Date().toUTCString(),
    name: game.name,
    steamUrl: vendorsUrl,
    steamID: id,
    inPackages: game.packages || null,
    isDLC: game.type === 'dlc',
    description: game.short_description || null,
    headerImage: game.header_image || null,
    initialPrice: initialAmount,
    initialCurrency: currency,
    offers: offers,
    isReleased: isReleased,
  };
}

export async function scrapeBatch(ids: number[], region?: eRegion): Promise<scrapeResult> {
  let result = new scrapeResult([]);

  for (let i = 0; i < ids.length; i++) {
    const id = ids[i];
    try {
      const res = await fetchSteamGame(id, region);

      if (res instanceof HttpStatusError) {
        logger.error(`Stopping batch: received HTTP ${res.status} for Steam id ${id}`, res.body ?? res.message);
        result.err = res;
        result.unprocessedIds = ids.slice(i);
        return result;
      }

      if (res !== null) result.res.push(res);
    } catch (err) {
      logger.error(`Unexpected error while fetching game ${id}:`, err);
    }
  }

  return result;
}
export class scrapeResult {
  res: Game[];
  err?: HttpStatusError;
  unprocessedIds?: number[];

  constructor(res: Game[], err?: HttpStatusError, unprocessedIds?: number[]) {
    this.res = res;
    this.err = err;
    this.unprocessedIds = unprocessedIds;
  }
}
