import { eCurrency } from "../enums/eCurrency.js";
import { eVendor } from "../enums/eVendor.js";
import { Entity } from "./entity.js";


export interface GameOffer extends Entity {
  gameId: string;
  vendorsGameId: string;
  vendor: eVendor;
  vendorsUrl: string;
  available: boolean;
  amount: number | null;
  currency: eCurrency | null;
}