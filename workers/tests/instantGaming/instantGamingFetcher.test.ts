import { beforeEach, describe, expect, it, vi } from "vitest";
import { validate as isUuid, version as uuidVersion } from "uuid";
import { fetchInstantGamingOffer } from "../../src/instantGaming/instantGamingFetcher.js";
import { FetchFailureStatus, fetchHTML, HttpStatusError, WrongDataError } from "../../src/utils/fetch.js";
import { checkGameOfferExists, getOfferId } from "../../src/backendUtils.js";
import getSteamAndGameIdsFromHtml from "../../src/utils/getSteamIdFromHtml.js";
import { eCurrency } from "../../src/utils/types/enums/eCurrency.js";
import { eVendor } from "../../src/utils/types/enums/eVendor.js";
import { isGameOffer } from "../../src/utils/types/entities/gameOffer.js";
import { buildInstantGamingPage } from "../fixtures/instantGamingHtml.js";

vi.mock("../../src/utils/fetch.js", async importOriginal => ({
  ...(await importOriginal<typeof import("../../src/utils/fetch.js")>()),
  fetchHTML: vi.fn(),
}));

vi.mock("../../src/backendUtils.js", () => ({
  checkGameOfferExists: vi.fn(),
  getOfferId: vi.fn(),
}));

vi.mock("../../src/utils/getSteamIdFromHtml.js", async importOriginal => ({
  ...(await importOriginal<typeof import("../../src/utils/getSteamIdFromHtml.js")>()),
  default: vi.fn(),
}));

const fetchHTMLMock = vi.mocked(fetchHTML);
const checkGameOfferExistsMock = vi.mocked(checkGameOfferExists);
const getOfferIdMock = vi.mocked(getOfferId);
const getSteamAndGameIdsMock = vi.mocked(getSteamAndGameIdsFromHtml);

const VENDOR_ID = 7741;
const GAME_ID = "5f0c5f4e-3c1a-4d8e-9a59-2f6a8a0c1b11";
const EXISTING_OFFER_ID = "0b8f0f7e-8d0a-4c47-9a2e-0d8e6f1a2b33";
const CANONICAL_URL = "https://www.instant-gaming.com/en/7741-buy-cyberpunk-2077-pc-steam/";

describe("fetchInstantGamingOffer", () => {
  beforeEach(() => {
    fetchHTMLMock.mockReset().mockResolvedValue(buildInstantGamingPage({ canonicalUrl: CANONICAL_URL }));
    getSteamAndGameIdsMock.mockReset().mockResolvedValue({ steamId: 1091500, gameId: GAME_ID });
    checkGameOfferExistsMock.mockReset().mockResolvedValue(false);
    getOfferIdMock.mockReset().mockResolvedValue(null);
  });

  describe("request", () => {
    it("requests the product page in the given currency through the proxy", async () => {
      await fetchInstantGamingOffer(VENDOR_ID, eCurrency.USD, "http://proxy.test:8080");

      expect(fetchHTMLMock).toHaveBeenCalledWith("https://www.instant-gaming.com/7741-/?currency=USD", "http://proxy.test:8080");
    });

    it("defaults to EUR when no currency is given", async () => {
      await fetchInstantGamingOffer(VENDOR_ID);

      expect(fetchHTMLMock).toHaveBeenCalledWith("https://www.instant-gaming.com/7741-/?currency=EUR", undefined);
    });

    it("propagates HTTP errors from the page fetch", async () => {
      fetchHTMLMock.mockRejectedValue(new HttpStatusError(429, "Too many requests"));

      await expect(fetchInstantGamingOffer(VENDOR_ID)).rejects.toMatchObject({ name: "HttpStatusError", status: 429 });
      expect(getSteamAndGameIdsMock).not.toHaveBeenCalled();
    });
  });

  describe("validation", () => {
    it("rejects products that are not PC games", async () => {
      fetchHTMLMock.mockResolvedValue(buildInstantGamingPage({ title: "Forza Horizon 5 - Xbox Series X|S" }));

      await expect(fetchInstantGamingOffer(VENDOR_ID)).rejects.toBeInstanceOf(WrongDataError);
      expect(getSteamAndGameIdsMock).not.toHaveBeenCalled();
      expect(checkGameOfferExistsMock).not.toHaveBeenCalled();
    });

    it("rejects offers that cannot be linked to a game", async () => {
      getSteamAndGameIdsMock.mockResolvedValue(null);

      await expect(fetchInstantGamingOffer(VENDOR_ID)).rejects.toBeInstanceOf(WrongDataError);
      expect(checkGameOfferExistsMock).not.toHaveBeenCalled();
    });

    it("rejects offers whose Steam id is unknown to the backend", async () => {
      getSteamAndGameIdsMock.mockResolvedValue({ steamId: 1091500, gameId: null as unknown as string });

      await expect(fetchInstantGamingOffer(VENDOR_ID)).rejects.toBeInstanceOf(WrongDataError);
    });
  });

  describe("offer id", () => {
    it("generates a uuid v4 for a new offer", async () => {
      const offer = await fetchInstantGamingOffer(VENDOR_ID);

      expect(isUuid(offer.id) && uuidVersion(offer.id)).toBe(4);
      expect(checkGameOfferExistsMock).toHaveBeenCalledWith({ vendorId: "7741" }, eVendor.InstatnGaming);
      expect(getOfferIdMock).not.toHaveBeenCalled();
    });

    it("reuses the existing offer id found by game id", async () => {
      checkGameOfferExistsMock.mockResolvedValue(true);
      getOfferIdMock.mockResolvedValue(EXISTING_OFFER_ID);

      const offer = await fetchInstantGamingOffer(VENDOR_ID);

      expect(offer.id).toBe(EXISTING_OFFER_ID);
      expect(getOfferIdMock).toHaveBeenCalledTimes(1);
      expect(getOfferIdMock).toHaveBeenCalledWith({ gameId: GAME_ID }, eVendor.InstatnGaming);
    });

    it("falls back to the vendor id lookup when the game id lookup finds nothing", async () => {
      checkGameOfferExistsMock.mockResolvedValue(true);
      getOfferIdMock.mockResolvedValueOnce(null).mockResolvedValueOnce(EXISTING_OFFER_ID);

      const offer = await fetchInstantGamingOffer(VENDOR_ID);

      expect(offer.id).toBe(EXISTING_OFFER_ID);
      expect(getOfferIdMock).toHaveBeenNthCalledWith(2, { vendorId: "7741" }, eVendor.InstatnGaming);
    });

    // FIXME: when the offer exists but both id lookups fail, the offer is returned with a null id.
    it("returns an offer with a null id when both lookups fail (current behavior)", async () => {
      checkGameOfferExistsMock.mockResolvedValue(true);

      const offer = await fetchInstantGamingOffer(VENDOR_ID);

      expect(offer.id).toBeNull();
    });

    it("throws a retryable error instead of generating a new id when the existence check fails", async () => {
      checkGameOfferExistsMock.mockResolvedValue(null);

      await expect(fetchInstantGamingOffer(VENDOR_ID)).rejects.toMatchObject({
        name: "HttpStatusError",
        status: FetchFailureStatus.NetworkError,
      });
      expect(getOfferIdMock).not.toHaveBeenCalled();
    });
  });

  describe("offer fields", () => {
    it("builds a complete game offer from the page", async () => {
      const offer = await fetchInstantGamingOffer(VENDOR_ID, eCurrency.EUR);

      expect(isGameOffer(offer)).toBe(true);
      expect(offer).toMatchObject({
        gameId: GAME_ID,
        vendorsGameId: "7741",
        vendor: eVendor.InstatnGaming,
        vendorsUrl: CANONICAL_URL,
        available: true,
        amount: 19.99,
        currency: eCurrency.EUR,
        offerName: "Cyberpunk 2077 - PC - Steam",
      });
      expect(Number.isNaN(Date.parse(offer.createdAt))).toBe(false);
      expect(Number.isNaN(Date.parse(offer.updatedAt))).toBe(false);
    });

    it("marks out-of-stock offers as unavailable", async () => {
      fetchHTMLMock.mockResolvedValue(buildInstantGamingPage({ noStock: true }));

      const offer = await fetchInstantGamingOffer(VENDOR_ID);

      expect(offer.available).toBe(false);
    });

    it("stores a null price and currency when the price cannot be parsed", async () => {
      fetchHTMLMock.mockResolvedValue(buildInstantGamingPage({ price: null }));

      const offer = await fetchInstantGamingOffer(VENDOR_ID);

      expect(offer.amount).toBeNull();
      expect(offer.currency).toBeNull();
      expect(isGameOffer(offer)).toBe(true);
    });

    it("takes the currency from the price symbol, not from the request", async () => {
      fetchHTMLMock.mockResolvedValue(buildInstantGamingPage({ price: "$21.49" }));

      const offer = await fetchInstantGamingOffer(VENDOR_ID, eCurrency.EUR);

      expect(offer).toMatchObject({ amount: 21.49, currency: eCurrency.USD });
    });

    it("uses the request URL when the page has no canonical link", async () => {
      fetchHTMLMock.mockResolvedValue(buildInstantGamingPage({ canonicalUrl: null }));

      const offer = await fetchInstantGamingOffer(VENDOR_ID, eCurrency.USD);

      expect(offer.vendorsUrl).toBe("https://www.instant-gaming.com/7741-/?currency=USD");
    });
  });
});
