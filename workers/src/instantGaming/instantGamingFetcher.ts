import { v4 } from "uuid";
import { fetchHTML, HttpStatusError, parseHtmlToDocument } from "../utils/offerFetcher.js";
import { GameOffer } from "../utils/types/entities/gameOffer.js";
import { eCurrency } from "../utils/types/enums/eCurrency.js";
import { getCanonicalIGurl, getFirstSteamIdFromMediaSourceIG } from "../utils/instantGaminghHelpers.js";
import { eVendor } from "../utils/types/enums/eVendor.js";
import { findNoStockElementIG, findPriceElementIG } from "../utils/instantGaminghHelpers.js";
import logger from "../utils/logger.js";
import { checkIgOfferExists, getIgOfferId } from "../backendUtils.js";
import getSteamAndGameIdsFromHtml, { getVendorsGameNameFromHtml } from "../utils/getSteamIdFromHtml.js";

export async function fetchInstantGamingOffer(id: number, currency?: eCurrency, proxy?: string): Promise<GameOffer | null | HttpStatusError> {
  const url = `https://www.instant-gaming.com/${encodeURIComponent(id)}-/?currency=${encodeURIComponent(currency?.toString().toUpperCase() || 'EUR')}`;
  
  const data = await fetchHTML(url);
  if (!data) return null;

  const steamAndGameIds = await getSteamAndGameIdsFromHtml(data);
  if (!steamAndGameIds || !steamAndGameIds.gameId) {
    logger.error(`❌Could not extract gameId from HTML of ${url}`);
    return null;
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
