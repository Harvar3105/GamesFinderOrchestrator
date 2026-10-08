import { GameStoreMetaData } from "../../../steam/gameMetadataFetcher.js";
import { eCurrency } from "../enums/eCurrency.js";
import { isEnumValue, isNumberArray, isObject, isStringArray } from "../typeGuards.js";
import { Entity, isEntity } from "./entity.js";
import { GameOffer, isGameOffer } from "./gameOffer.js";

export interface Game extends Entity {
  name: string;
  steamUrl: string;
  steamID: number;
  inPackages: number[] | null;
  isDLC: boolean;
  description: string | null;
  headerImage: string | null;
  offers: GameOffer[] | null;
  isReleased: boolean;
  initialPrice: number | null;
  initialCurrency: eCurrency | null;
  storeMetadata?: GameStoreMetaData | null;
}

function isGameStoreMetadata(value: unknown): value is GameStoreMetaData {
  if (!isObject(value)) return false;

  return (
    isStringArray(value.tags) &&
    isStringArray(value.genres) &&
    typeof value.positiveReviews === "number" &&
    typeof value.negativeReviews === "number" &&
    typeof value.positiveReviewsPercent === "number"
  );
}

export function isGame(value: unknown): value is Game {
  if (!isObject(value) || !isEntity(value)) return false;

  return (
    typeof value.name === "string" &&
    typeof value.steamUrl === "string" &&
    typeof value.steamID === "number" &&
    (isNumberArray(value.inPackages) || value.inPackages === null) &&
    typeof value.isDLC === "boolean" &&
    (typeof value.description === "string" || value.description === null) &&
    (typeof value.headerImage === "string" || value.headerImage === null) &&
    ((Array.isArray(value.offers) && value.offers.every(isGameOffer)) || value.offers === null) &&
    typeof value.isReleased === "boolean" &&
    (typeof value.initialPrice === "number" || value.initialPrice === null) &&
    (isEnumValue(eCurrency, value.initialCurrency) || value.initialCurrency === null) &&
    (
      value.storeMetadata === undefined ||
      value.storeMetadata === null ||
      isGameStoreMetadata(value.storeMetadata)
    )
  );
}
