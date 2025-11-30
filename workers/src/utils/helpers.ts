import { eCurrency, getECurrencyFromString } from "../utils/types/enums/eCurrency.js";

export function splitIntoBatches<T>(array: T[], batchSize: number): T[][] {
  const batches: T[][] = [];
  for (let i = 0; i < array.length; i += batchSize) {
    batches.push(array.slice(i, i + batchSize));
  }
  return batches;
}

export function getSteamIdsFromMediaSources(html: string): number[] | null {
  const regex = /apps\/(\d+)\/extras/gi;
  const matches = [...html.matchAll(regex)];
  return matches.map(m => Number(m[1]));
}

export function getFirstSteamIdFromMediaSource(html: string): number | null {
  const match = html.match(/apps\/(\d+)\/extras/i);
  return match ? Number(match[1]) : null;
}

export function findNoStockElement(root: Document | Element | null | undefined): Element | null {
  if (!root) return null;
  try {
    return (root as Document | Element).querySelector('.noStock');
  } catch (e) {
    return null;
  }
}

export function findPriceElement(root: Document | Element | null | undefined): { currency: eCurrency; price: number } | null {
  if (!root) return null;
  try {
    const element = (root as Document | Element).querySelector('.total');
    if (!element) return null;
    const priceText = element.textContent || '';

    const currencySymbol = priceText.charAt(0);
    const currency = getECurrencyFromString(currencySymbol);
    if (!currency) return null;

    const price = parseFloat(priceText.substring(1).trim());
    if (Number.isNaN(price)) return null;
    
    return { currency, price };
  } catch (e) {
    return null;
  }
}