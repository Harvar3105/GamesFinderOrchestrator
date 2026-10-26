import { v4 } from "uuid";
import { Game } from "../utils/types/entities/game.js";
import { eCurrency, getECurrencyFromString } from "../utils/types/enums/eCurrency.js";
import { eRegion } from "../utils/types/enums/eRegion.js";
import { eVendor } from "../utils/types/enums/eVendor.js";
import { GameOffer } from "../utils/types/entities/gameOffer.js";
import logger from "../utils/logger.js";
import { fetchJson, HttpStatusError, WrongDataError } from "../utils/offerFetcher.js";
import { checkGameExists, checkSteamOfferExists, getGameIdBySteamIdAsync, getSteamOfferId } from "../backendUtils.js";
import { fetchGameStoreMetadata } from "./gameMetadataFetcher.js";
import { config } from "../utils/config.js";

export class SkippedError extends Error {
  constructor(){
    super("Skipped");
  }
}

export async function fetchSteamGame(id: number, updateGame: boolean, updateDeal: boolean, region: eRegion = eRegion.US ): Promise<Game | GameOffer> {
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

  let offerId;
  if (offerExists) offerId = await getSteamOfferId({gameId: gameId!})?? await getSteamOfferId({vendorId: id.toString()});
  else offerId = v4();

  if (gameExists && offerExists && !updateGame && !updateDeal) throw new SkippedError();
  
  const data = await fetchJson({url: url, timeoutMS: config.steamApiTimeoutMs});
  if (!data?.[id]?.success) throw new WrongDataError(`No data were returned. ${data?.[id]}`);

  const game = data[id].data;

  const isReleased = !game.release_date.coming_soon;
  let offers = null;

  const vendorsUrl = `https://store.steampowered.com/app/${id}`

  let currency: eCurrency | null = null;
  let initialAmount: number | null = null;

  if (!offerExists || (offerExists && updateDeal)) {
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
        currency: currency,
        offerName: game.name
      } as GameOffer]
    } catch (e) {
      logger.error(`❌Error parsing price for game ID ${id}:`, e);
    }
  }

  if (!gameExists || (gameExists && updateGame)) {
    let storeMetadata = await fetchGameStoreMetadata(id);
    if (storeMetadata instanceof HttpStatusError) {
      logger.error(`💥Error fetching metadata for game ID ${id}:`, storeMetadata);
      storeMetadata = null;
    }

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
    storeMetadata: storeMetadata
    };
  }
  if (offers) return offers[0];
  throw new SkippedError();
}
