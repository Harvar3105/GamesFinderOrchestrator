import { eCurrency } from "../enums/eCurrency.js";
import { eVendor } from "../enums/eVendor.js";
import { isEnumValue, isObject } from "../typeGuards.js";
import { Entity, isEntity } from "./entity.js";


export interface GameOffer extends Entity {
  gameId: string;
  vendorsGameId: string;
  vendor: eVendor;
  vendorsUrl: string;
  available: boolean;
  amount: number | null;
  currency: eCurrency | null;
  offerName: string;
}

export function isGameOffer(value: unknown): value is GameOffer {
  if (!isObject(value) || !isEntity(value)) return false;

  return (
    typeof value.gameId === "string" &&
    typeof value.vendorsGameId === "string" &&
    isEnumValue(eVendor, value.vendor) &&
    typeof value.vendorsUrl === "string" &&
    typeof value.available === "boolean" &&
    (typeof value.amount === "number" || value.amount === null) &&
    (isEnumValue(eCurrency, value.currency) || value.currency === null) &&
    typeof value.offerName === "string"
  );
}
