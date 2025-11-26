import { v4 } from "uuid";
import { Game } from "../utils/types/entities/game.js";
import { eCurrency, getECurrencyFromString } from "../utils/types/enums/eCurrency.js";
import { eRegion } from "../utils/types/enums/eRegion.js";
import { eVendor } from "../utils/types/enums/eVendor.js";
import { GameOffer } from "../utils/types/entities/gameOffer.js";
import logger from "../utils/logger.js";
import { fetchJson } from "../utils/offerFetcher.js";

export async function fetchSteamGame(id: number, region: eRegion = eRegion.US ): Promise<Game | null> {
  const url = `https://store.steampowered.com/api/appdetails?appids=${id}&cc=${region}&l=en`;
  const data = await fetchJson(url);

  if (!data[id]?.success) return null;
  const game = data[id].data;

  const gameId = v4();
  const isReleased = !game.release_date.coming_soon;
  let initialPrice = null;
  let offers = null;

  const vendorsUrl = `https://store.steampowered.com/app/${id}`

  if (isReleased){
    try {
      // Might depend on region but due to steam api poor typization we do it like this
      const currency = getECurrencyFromString(game.price_overview?.currency || 'USD')!; 
      const initialAmount = Number((game.price_overview?.initial / 100).toFixed(2));
      initialPrice = {[currency]: initialAmount} as Record<eCurrency, number>;

      const currentAmount = Number((game.price_overview?.final / 100).toFixed(2));
      offers = [{
        id: v4(),
        createdAt: new Date().toUTCString(),
        updatedAt: new Date().toUTCString(),
        gameId: gameId,
        vendorsGameId: gameId.toString(),
        vendor: eVendor.Steam,
        vendorsUrl: vendorsUrl,
        available: true,
        price: {[currency]: currentAmount} as Record<eCurrency, number>,
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
    initialPrices: initialPrice,
    offers: offers,
    isReleased: isReleased,
  };
}

export async function scrapeBatch(ids: number[], region?: eRegion): Promise<Game[]> {
  const results: Game[] = [];

  results.push(
    ...(await Promise.all(
      ids.map(id => fetchSteamGame(id, region))
    ))
    .filter(g => g !== null) as Game[]
  );

  return results;
}
