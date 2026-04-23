import { v4 } from "uuid";
import { Game } from "../utils/types/entities/game.js";
import { eCurrency, getECurrencyFromString } from "../utils/types/enums/eCurrency.js";
import { eRegion } from "../utils/types/enums/eRegion.js";
import { eVendor } from "../utils/types/enums/eVendor.js";
import { GameOffer } from "../utils/types/entities/gameOffer.js";
import logger from "../utils/logger.js";
import { fetchJson, HttpStatusError } from "../utils/offerFetcher.js";
import { checkGameExists, checkSteamOfferExists, getGameIdBySteamIdAsync, getSteamOfferId } from "../backendUtils.js";

export async function fetchSteamGame(id: number, updateGame: boolean, updateDeal: boolean, region: eRegion = eRegion.US ): Promise<Game | GameOffer | null | HttpStatusError> {
  const url = `https://store.steampowered.com/api/appdetails?appids=${id}&cc=${region}&l=en`;

  let offerExists = await checkSteamOfferExists(id);
  let gameExists = await checkGameExists(id);

  let gameId;
  if (gameExists) {
    gameId = await getGameIdBySteamIdAsync(id);
  }
  else {
    gameId = v4();
  }

  logger.info(`➡️ Game Exists: ${gameExists}\nGame ID: ${gameId}`);

  let offerId;
  if (offerExists) offerId = await getSteamOfferId({gameId: gameId!})?? await getSteamOfferId({vendorId: id.toString()});
  else offerId = v4();

  if (gameExists && offerExists && !updateGame && !updateDeal) return new HttpStatusError(0, "");
  
  let data;
  try {
    data = await fetchJson(url);
  } catch (err) {
    if (err instanceof HttpStatusError) return err;
    return null;
  }

  if (!data[id]?.success) return null;

  const game = data[id].data;

  const isReleased = !game.release_date.coming_soon;
  let offers = null;

  const vendorsUrl = `https://store.steampowered.com/app/${id}`

  let currency: eCurrency | null = null;
  let initialAmount: number | null = null;

  // TODO: Add not released games as a preorder
  if (isReleased && (!offerExists || (offerExists && updateDeal))) {
    try {
      // Might depend on region but due to steam api poor typization we do it like this
      currency = getECurrencyFromString(game.price_overview?.currency || 'null')!; 
      initialAmount = Number((game.price_overview?.initial / 100).toFixed(2));

      const currentAmount = Number((game.price_overview?.final / 100).toFixed(2));
      offers = [{
        id: offerId,
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

  if (!gameExists || (gameExists && updateGame)) {
    return {
    id: gameId!,
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
  return offers ? offers[0] : null;
}

export async function scrapeBatch(ids: number[], updateGames: boolean = true, updateDeals: boolean = true, region?: eRegion): Promise<scrapeResult> {
  let result = new scrapeResult();

  for (let i = 0; i < ids.length; i++) {
    const id = ids[i];
    try {
      const res = await fetchSteamGame(id, updateGames, updateDeals, region);

      if (res instanceof HttpStatusError) {
        if (res.status === 0){
          result.skippedIds ++;
          continue;
        }

        logger.error(`⚠️Stopping batch: received HTTP ${res.status} for Steam id ${id}`);
        result.err = res;
        result.unprocessedIds = ids.slice(i);
        return result;
      }

      if (!res) continue;

      if ('steamID' in res) {
        result.games.push(res);
      } else {
        result.offers.push(res);
      }
      
    } catch (err) {
      logger.error(`Unexpected error while fetching game ${id}:`, err);
    }
  }

  return result;
}
export class scrapeResult {
  games: Game[] = [];
  offers: GameOffer[] = [];
  err?: HttpStatusError;
  unprocessedIds?: number[];
  skippedIds: number = 0;

  constructor(err?: HttpStatusError, unprocessedIds?: number[]) {
    this.err = err;
    this.unprocessedIds = unprocessedIds;
  }
}
