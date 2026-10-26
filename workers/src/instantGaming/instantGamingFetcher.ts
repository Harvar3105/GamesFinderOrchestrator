import { v4 } from "uuid";
import { fetchHTML, HttpStatusError, parseHtmlToDocument, WrongDataError } from "../utils/offerFetcher.js";
import { GameOffer } from "../utils/types/entities/gameOffer.js";
import { eCurrency } from "../utils/types/enums/eCurrency.js";
import { checkIfPCGameIG, getCanonicalIGurl } from "../utils/instantGaminghHelpers.js";
import { eVendor } from "../utils/types/enums/eVendor.js";
import { findNoStockElementIG, findPriceElementIG } from "../utils/instantGaminghHelpers.js";
import logger from "../utils/logger.js";
import { checkIgOfferExists, getIgOfferId } from "../backendUtils.js";
import getSteamAndGameIdsFromHtml, { getVendorsGameNameFromHtml } from "../utils/getSteamIdFromHtml.js";

export async function fetchInstantGamingOffer(id: number, currency?: eCurrency, proxy?: string): Promise<GameOffer> {
  const url = `https://www.instant-gaming.com/${encodeURIComponent(id)}-/?currency=${encodeURIComponent(currency?.toString().toUpperCase() || 'EUR')}`;
  const data = await fetchHTML(url);
  
  //TODO: Consider to use multiplatform?
  if (checkIfPCGameIG(data) === false) {
    logger.info(`⛔ Skipping offer ${url} because it is not a PC game`);
    throw new WrongDataError("Wrong platform recieved.");
  }
  
  const steamAndGameIds = await getSteamAndGameIdsFromHtml(data);
  if (!steamAndGameIds || !steamAndGameIds.gameId) {
    logger.error(`❌Could not extract gameId from HTML of ${url}`);
    throw new WrongDataError("Could not get gameId from html and backend.");
  }

  let offerExists = await checkIgOfferExists(id.toString());
  let offerId;
  if (offerExists) {
    offerId = await getIgOfferId({gameId: steamAndGameIds.gameId!})?? await getIgOfferId({vendorId: id.toString()});
  }
  else {
    offerId = v4();
  }

  const doc = parseHtmlToDocument(data);
  var priceElement = findPriceElementIG(doc);
  const canonicalUrl = getCanonicalIGurl(doc) ?? url;

  var gameOffer: GameOffer = {
    id: offerId!,
    createdAt: new Date().toUTCString(),
    updatedAt: new Date().toUTCString(),
    gameId: steamAndGameIds.gameId,
    vendorsGameId: id.toString(),
    vendor: eVendor.InstatnGaming,
    vendorsUrl: canonicalUrl,
    available: findNoStockElementIG(doc) ? false : true,
    amount: priceElement?.price ?? null,
    currency: priceElement?.currency ?? null,
    offerName: getVendorsGameNameFromHtml(data) ?? `UNKNOWN NAME - ${id}`
  }

  return gameOffer;
}
