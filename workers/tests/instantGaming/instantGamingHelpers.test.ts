import { describe, expect, it } from "vitest";
import {
  checkIfPCGameIG,
  findNoStockElementIG,
  findPriceElementIG,
  getCanonicalIGurl,
  getFirstSteamIdFromMediaSourceIG,
  getSteamIdsFromMediaSourcesIG,
  splitIntoBatches,
} from "../../src/utils/instantGaminghHelpers.js";
import { parseHtmlToDocument } from "../../src/utils/offerFetcher.js";
import { eCurrency } from "../../src/utils/types/enums/eCurrency.js";
import { buildInstantGamingPage, InstantGamingPageOptions } from "../fixtures/instantGamingHtml.js";

const pageDocument = (options: InstantGamingPageOptions = {}) => parseHtmlToDocument(buildInstantGamingPage(options));

describe("splitIntoBatches", () => {
  it("splits an array into batches and keeps the remainder in the last batch", () => {
    expect(splitIntoBatches([1, 2, 3, 4, 5], 2)).toEqual([[1, 2], [3, 4], [5]]);
  });

  it("returns a single batch when the batch size exceeds the array length", () => {
    expect(splitIntoBatches([1, 2], 10)).toEqual([[1, 2]]);
  });

  it("returns no batches for an empty array", () => {
    expect(splitIntoBatches([], 3)).toEqual([]);
  });
});

describe("getSteamIdsFromMediaSourcesIG", () => {
  it("extracts every Steam app id from media URLs", () => {
    const html = buildInstantGamingPage({ steamMediaIds: [1091500, 2050650] });
    expect(getSteamIdsFromMediaSourcesIG(html)).toEqual([1091500, 2050650]);
  });

  it("returns an empty array when there are no Steam media URLs", () => {
    const html = buildInstantGamingPage({ steamMediaIds: [] });
    expect(getSteamIdsFromMediaSourcesIG(html)).toEqual([]);
  });
});

describe("getFirstSteamIdFromMediaSourceIG", () => {
  it("returns the first Steam app id", () => {
    const html = buildInstantGamingPage({ steamMediaIds: [1091500, 2050650] });
    expect(getFirstSteamIdFromMediaSourceIG(html)).toBe(1091500);
  });

  it("returns null when there are no Steam media URLs", () => {
    const html = buildInstantGamingPage({ steamMediaIds: [] });
    expect(getFirstSteamIdFromMediaSourceIG(html)).toBeNull();
  });

  it("ignores Steam URLs that are not media extras", () => {
    expect(getFirstSteamIdFromMediaSourceIG(`<a href="https://store.steampowered.com/app/570/">Dota</a>`)).toBeNull();
  });
});

describe("findNoStockElementIG", () => {
  it("finds the out-of-stock marker", () => {
    expect(findNoStockElementIG(pageDocument({ noStock: true }))).not.toBeNull();
  });

  it("returns null when the product is in stock", () => {
    expect(findNoStockElementIG(pageDocument({ noStock: false }))).toBeNull();
  });

  it("returns null for a missing root", () => {
    expect(findNoStockElementIG(null)).toBeNull();
    expect(findNoStockElementIG(undefined)).toBeNull();
  });
});

describe("findPriceElementIG", () => {
  it("parses a euro price", () => {
    expect(findPriceElementIG(pageDocument({ price: "19.99€" }))).toEqual({ currency: eCurrency.EUR, price: 19.99 });
  });

  it("parses a dollar price", () => {
    expect(findPriceElementIG(pageDocument({ price: "$5.49" }))).toEqual({ currency: eCurrency.USD, price: 5.49 });
  });

  it("parses an integer price", () => {
    expect(findPriceElementIG(pageDocument({ price: "40 €" }))).toEqual({ currency: eCurrency.EUR, price: 40 });
  });

  it("returns null when the currency symbol is unknown", () => {
    expect(findPriceElementIG(pageDocument({ price: "19.99 zł" }))).toBeNull();
  });

  it("returns null when there is no price element", () => {
    expect(findPriceElementIG(pageDocument({ price: null }))).toBeNull();
  });

  it("returns null when the price element has no number", () => {
    expect(findPriceElementIG(pageDocument({ price: "€" }))).toBeNull();
  });

  // FIXME: a zero price is treated as "no price" because of the falsy check in findPriceElementIG.
  it("returns null for a zero price (current behavior)", () => {
    expect(findPriceElementIG(pageDocument({ price: "0.00€" }))).toBeNull();
  });

  it("returns null for a missing root", () => {
    expect(findPriceElementIG(null)).toBeNull();
  });
});

describe("getCanonicalIGurl", () => {
  it("returns the canonical link", () => {
    const url = "https://www.instant-gaming.com/en/7741-buy-cyberpunk-2077-pc-steam/";
    expect(getCanonicalIGurl(pageDocument({ canonicalUrl: url }))).toBe(url);
  });

  it("returns null when there is no canonical link", () => {
    expect(getCanonicalIGurl(pageDocument({ canonicalUrl: null }))).toBeNull();
  });

  it("returns null for a missing root", () => {
    expect(getCanonicalIGurl(undefined)).toBeNull();
  });
});

describe("checkIfPCGameIG", () => {
  it("accepts PC products regardless of case", () => {
    expect(checkIfPCGameIG(buildInstantGamingPage({ title: "Cyberpunk 2077 - PC - Steam" }))).toBe(true);
    expect(checkIfPCGameIG(buildInstantGamingPage({ title: "Elden Ring - Pc - Steam" }))).toBe(true);
  });

  it("rejects console products", () => {
    expect(checkIfPCGameIG(buildInstantGamingPage({ title: "Forza Horizon 5 - Xbox Series X|S" }))).toBe(false);
  });

  it("rejects pages without a title", () => {
    expect(checkIfPCGameIG(buildInstantGamingPage({ title: null }))).toBe(false);
  });

  // FIXME: "pc" is matched as a substring, so non-PC titles that contain it are accepted.
  it("accepts a console product whose name contains 'pc' (current behavior)", () => {
    expect(checkIfPCGameIG(buildInstantGamingPage({ title: "Upcycle Tycoon - Xbox One" }))).toBe(true);
  });
});
