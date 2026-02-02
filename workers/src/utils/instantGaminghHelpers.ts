import { eCurrency, getECurrencyBySymbol, getECurrencyFromString } from "./types/enums/eCurrency.js";

export function splitIntoBatches<T>(array: T[], batchSize: number): T[][] {
  const batches: T[][] = [];
  for (let i = 0; i < array.length; i += batchSize) {
    batches.push(array.slice(i, i + batchSize));
  }
  return batches;
}

export function getSteamIdsFromMediaSourcesIG(html: string): number[] | null {
  const regex = /apps\/(\d+)\/extras/gi;
  const matches = [...html.matchAll(regex)];
  return matches.map(m => Number(m[1]));
}

export function getFirstSteamIdFromMediaSourceIG(html: string): number | null {
  const match = html.match(/apps\/(\d+)\/extras/i);
  return match ? Number(match[1]) : null;
}

export function findNoStockElementIG(root: Document | Element | null | undefined): Element | null {
  if (!root) return null;
  try {
    return (root as Document | Element).querySelector('.noStock');
  } catch (e) {
    return null;
  }
}

export function findPriceElementIG(root: Document | Element | null | undefined): { currency: eCurrency; price: number } | null {
  if (!root) return null;
  try {
    const element = (root as Document | Element).querySelector('.total');
    if (!element) return null;
    const priceText = element.textContent || '';

    const currency = getECurrencyBySymbol(priceText);
    if (!currency) return null;

    const priceMatch = priceText.match(/\d+(?:\.\d+)?/);
    const value = priceMatch ? Number(priceMatch[0]) : null;
    if (!value) return null;
    
    return { currency: currency, price: value };
  } catch (e) {
    return null;
  }
}

export function getCanonicalIGurl(root: Document | Element | null | undefined): string | null {
  if (!root) return null;
  try {
    const element = (root as Document | Element).querySelector('link[rel="canonical"]');
    if (!element) return null;
    return element.getAttribute('href');
  } catch (e) {
    return null;
  }
}