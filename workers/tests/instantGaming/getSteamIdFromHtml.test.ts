import { beforeEach, describe, expect, it, vi } from "vitest";
import getSteamAndGameIdsFromHtml, { getVendorsGameNameFromHtml } from "../../src/utils/getSteamIdFromHtml.js";
import { fetchJson, HttpStatusError } from "../../src/utils/offerFetcher.js";
import { config } from "../../src/utils/config.js";
import { buildInstantGamingPage } from "../fixtures/instantGamingHtml.js";

vi.mock("../../src/utils/offerFetcher.js", async importOriginal => ({
  ...(await importOriginal<typeof import("../../src/utils/offerFetcher.js")>()),
  fetchJson: vi.fn(),
}));

const fetchJsonMock = vi.mocked(fetchJson);
const GAME_ID = "5f0c5f4e-3c1a-4d8e-9a59-2f6a8a0c1b11";

describe("getVendorsGameNameFromHtml", () => {
  it("returns the product title", () => {
    expect(getVendorsGameNameFromHtml(buildInstantGamingPage({ title: "Elden Ring - PC - Steam" }))).toBe("Elden Ring - PC - Steam");
  });

  it("returns null when there is no title", () => {
    expect(getVendorsGameNameFromHtml(buildInstantGamingPage({ title: null }))).toBeNull();
  });

  // FIXME: innerHTML keeps HTML entities, so "&" comes back as "&amp;". textContent would return the plain title.
  it("returns HTML-encoded entities in the title (current behavior)", () => {
    expect(getVendorsGameNameFromHtml(buildInstantGamingPage({ title: "Ratchet & Clank - PC" }))).toBe("Ratchet &amp; Clank - PC");
  });
});

describe("getSteamAndGameIdsFromHtml", () => {
  beforeEach(() => {
    fetchJsonMock.mockReset();
  });

  describe("when the page contains a Steam id", () => {
    it("resolves the game id by Steam id", async () => {
      fetchJsonMock.mockResolvedValue({ id: GAME_ID });

      const result = await getSteamAndGameIdsFromHtml(buildInstantGamingPage({ steamMediaIds: [1091500] }));

      expect(result).toEqual({ steamId: 1091500, gameId: GAME_ID });
      expect(fetchJsonMock).toHaveBeenCalledTimes(1);
      expect(fetchJsonMock).toHaveBeenCalledWith({
        url: `${config.backendUrl}${config.backendGetGameId}?steamId=1091500`,
        method: "GET",
        timeoutMS: config.backendTimeoutMs,
      });
    });

    it("does not fall back to the title lookup when the backend does not know the game", async () => {
      fetchJsonMock.mockResolvedValue({ id: null });

      const result = await getSteamAndGameIdsFromHtml(buildInstantGamingPage({ steamMediaIds: [1091500] }));

      expect(result).toEqual({ steamId: 1091500, gameId: null });
      expect(fetchJsonMock).toHaveBeenCalledTimes(1);
    });

    // FIXME: getGameId swallows transient backend errors (HttpStatusError), so the caller sees a missing gameId
    // and throws WrongDataError, which is not retried. The title lookup path lets the same error propagate.
    it("swallows backend errors and returns a null game id (current behavior)", async () => {
      fetchJsonMock.mockRejectedValue(new HttpStatusError(503, "Backend unavailable"));

      const result = await getSteamAndGameIdsFromHtml(buildInstantGamingPage({ steamMediaIds: [1091500] }));

      expect(result).toEqual({ steamId: 1091500, gameId: null });
    });
  });

  describe("when the page has no Steam id", () => {
    it("looks the game up by the title part before the first '-'", async () => {
      fetchJsonMock.mockResolvedValue({ exists: true, steamId: 1245620, gameId: GAME_ID });

      const result = await getSteamAndGameIdsFromHtml(
        buildInstantGamingPage({ title: "Elden Ring - PC - Steam", steamMediaIds: [] }),
      );

      expect(result).toEqual({ steamId: 1245620, gameId: GAME_ID });
      expect(fetchJsonMock).toHaveBeenCalledWith({
        url: `${config.backendUrl}${config.backendCheckGameExistsByName}?gameName=Elden%20Ring&getSteamId=true`,
        method: "GET",
        timeoutMS: config.backendTimeoutMs,
      });
    });

    it("returns null when the backend does not find the game", async () => {
      fetchJsonMock.mockResolvedValue({ exists: false });

      const result = await getSteamAndGameIdsFromHtml(buildInstantGamingPage({ steamMediaIds: [] }));

      expect(result).toBeNull();
    });

    it("returns null without calling the backend when there is no title", async () => {
      const result = await getSteamAndGameIdsFromHtml(buildInstantGamingPage({ title: null, steamMediaIds: [] }));

      expect(result).toBeNull();
      expect(fetchJsonMock).not.toHaveBeenCalled();
    });

    it("propagates backend errors so the offer can be retried", async () => {
      fetchJsonMock.mockRejectedValue(new HttpStatusError(503, "Backend unavailable"));

      await expect(getSteamAndGameIdsFromHtml(buildInstantGamingPage({ steamMediaIds: [] }))).rejects.toBeInstanceOf(HttpStatusError);
    });

    // FIXME: titles that contain '-' are cut at the first hyphen (see the TODO in getSteamIdFromHtml.ts).
    it("cuts hyphenated titles at the first '-' (current behavior)", async () => {
      fetchJsonMock.mockResolvedValue({ exists: false });

      await getSteamAndGameIdsFromHtml(buildInstantGamingPage({ title: "Spider-Man Remastered - PC - Steam", steamMediaIds: [] }));

      expect(fetchJsonMock.mock.calls[0][0].url).toContain("gameName=Spider&");
    });
  });
});
