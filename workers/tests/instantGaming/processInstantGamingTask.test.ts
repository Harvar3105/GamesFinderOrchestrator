import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import processInstantGamingTask, { processInstantGamingTaskIds } from "../../src/instantGaming/processInstantGamingTask.js";
import { fetchInstantGamingOffer } from "../../src/instantGaming/instantGamingFetcher.js";
import { HttpStatusError, WrongDataError } from "../../src/utils/offerFetcher.js";
import { config } from "../../src/utils/config.js";
import logger from "../../src/utils/logger.js";
import { GameOffer } from "../../src/utils/types/entities/gameOffer.js";
import { InstantGamingTask } from "../../src/utils/types/entities/tasks.js";
import { eCurrency } from "../../src/utils/types/enums/eCurrency.js";
import { eVendor } from "../../src/utils/types/enums/eVendor.js";

vi.mock("../../src/instantGaming/instantGamingFetcher.js", () => ({
  fetchInstantGamingOffer: vi.fn(),
}));

const fetchOfferMock = vi.mocked(fetchInstantGamingOffer);

function offerFor(vendorId: number): GameOffer {
  return {
    id: `offer-${vendorId}`,
    createdAt: "Mon, 26 Oct 2026 00:00:00 GMT",
    updatedAt: "Mon, 26 Oct 2026 00:00:00 GMT",
    gameId: `game-${vendorId}`,
    vendorsGameId: vendorId.toString(),
    vendor: eVendor.InstatnGaming,
    vendorsUrl: `https://www.instant-gaming.com/${vendorId}-/`,
    available: true,
    amount: 9.99,
    currency: eCurrency.EUR,
    offerName: `Game ${vendorId} - PC`,
  };
}

function buildTask(vendorsIds: number[]): InstantGamingTask {
  return {
    taskId: "task-1",
    createdAt: "2026-10-26T00:00:00Z",
    updateExistingDeals: true,
    redisResultKey: "ig:task-1",
    currency: eCurrency.USD,
    proxy: "http://proxy.test:8080",
    vendorsIds,
  };
}

/** Each id maps to the outcomes of consecutive fetches; ids without an entry always succeed. */
function scriptFetchOutcomes(outcomes: Record<number, Array<Error | "ok">>) {
  const attempts = new Map<number, number>();
  fetchOfferMock.mockImplementation(async id => {
    const attempt = attempts.get(id) ?? 0;
    attempts.set(id, attempt + 1);
    const outcome = outcomes[id]?.[attempt] ?? "ok";
    if (outcome instanceof Error) throw outcome;
    return offerFor(id);
  });
}

describe("processInstantGamingTaskIds", () => {
  beforeEach(() => {
    fetchOfferMock.mockReset();
  });

  it("fetches every id in order with the task currency and proxy", async () => {
    scriptFetchOutcomes({});

    const result = await processInstantGamingTaskIds([1, 2, 3], eCurrency.USD, "http://proxy.test:8080");

    expect(result.processed.map(o => o.vendorsGameId)).toEqual(["1", "2", "3"]);
    expect(result.unprocessed).toEqual([]);
    expect(fetchOfferMock.mock.calls).toEqual([
      [1, eCurrency.USD, "http://proxy.test:8080"],
      [2, eCurrency.USD, "http://proxy.test:8080"],
      [3, eCurrency.USD, "http://proxy.test:8080"],
    ]);
  });

  it("collects HTTP failures as unprocessed with their reason and continues", async () => {
    const error = new HttpStatusError(429, "Too many requests");
    scriptFetchOutcomes({ 2: [error] });

    const result = await processInstantGamingTaskIds([1, 2, 3]);

    expect(result.processed.map(o => o.vendorsGameId)).toEqual(["1", "3"]);
    expect(result.unprocessed).toEqual([{ id: 2, reason: error }]);
  });

  it("drops offers with wrong data without retrying them", async () => {
    scriptFetchOutcomes({ 2: [new WrongDataError("Wrong platform recieved.")] });

    const result = await processInstantGamingTaskIds([1, 2]);

    expect(result.processed.map(o => o.vendorsGameId)).toEqual(["1"]);
    expect(result.unprocessed).toEqual([]);
  });

  // FIXME: unexpected errors (e.g. a TypeError from a bug) are dropped silently, without a log entry.
  it("drops unexpected errors silently (current behavior)", async () => {
    scriptFetchOutcomes({ 1: [new TypeError("Cannot read properties of undefined")] });

    const result = await processInstantGamingTaskIds([1]);

    expect(result).toEqual({ processed: [], unprocessed: [] });
    expect(logger.error).not.toHaveBeenCalled();
    expect(logger.warn).not.toHaveBeenCalled();
  });

  it("returns empty results for an empty id list", async () => {
    expect(await processInstantGamingTaskIds([])).toEqual({ processed: [], unprocessed: [] });
    expect(fetchOfferMock).not.toHaveBeenCalled();
  });
});

describe("processInstantGamingTask", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    fetchOfferMock.mockReset();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("returns all offers without waiting when nothing fails", async () => {
    scriptFetchOutcomes({});

    const result = await processInstantGamingTask(buildTask([1, 2]), {});

    expect(result.map(o => o.vendorsGameId)).toEqual(["1", "2"]);
    expect(fetchOfferMock).toHaveBeenCalledTimes(2);
    expect(vi.getTimerCount()).toBe(0);
  });

  it("retries HTTP failures once after the cooldown", async () => {
    scriptFetchOutcomes({ 2: [new HttpStatusError(429, "Too many requests"), "ok"] });

    const resultPromise = processInstantGamingTask(buildTask([1, 2, 3]), {});

    await vi.advanceTimersByTimeAsync(config.cooldownMs - 1);
    expect(fetchOfferMock).toHaveBeenCalledTimes(3);

    await vi.advanceTimersByTimeAsync(1);
    const result = await resultPromise;

    expect(fetchOfferMock).toHaveBeenCalledTimes(4);
    expect(fetchOfferMock).toHaveBeenLastCalledWith(2, eCurrency.USD, "http://proxy.test:8080");
    expect(result.map(o => o.vendorsGameId)).toEqual(["1", "3", "2"]);
  });

  it("retries only the failed ids", async () => {
    scriptFetchOutcomes({ 1: [new HttpStatusError(503, "Network error"), "ok"] });

    const resultPromise = processInstantGamingTask(buildTask([1, 2]), {});
    await vi.runAllTimersAsync();
    await resultPromise;

    expect(fetchOfferMock.mock.calls.map(call => call[0])).toEqual([1, 2, 1]);
  });

  it("logs and skips ids that still fail after the retry", async () => {
    const error = new HttpStatusError(503, "Network error");
    scriptFetchOutcomes({ 2: [error, error] });

    const resultPromise = processInstantGamingTask(buildTask([1, 2]), {});
    await vi.runAllTimersAsync();
    const result = await resultPromise;

    expect(result.map(o => o.vendorsGameId)).toEqual(["1"]);
    expect(fetchOfferMock).toHaveBeenCalledTimes(3);
    expect(logger.warn).toHaveBeenCalledWith(expect.stringContaining("Offer 2 could not be processed for task task-1"), error);
  });

  it("does not retry offers with wrong data", async () => {
    scriptFetchOutcomes({ 1: [new WrongDataError("Wrong platform recieved.")] });

    const result = await processInstantGamingTask(buildTask([1, 2]), {});

    expect(result.map(o => o.vendorsGameId)).toEqual(["2"]);
    expect(fetchOfferMock).toHaveBeenCalledTimes(2);
    expect(vi.getTimerCount()).toBe(0);
  });

  // FIXME: every HttpStatusError is retried after the full cooldown, including 404 for IDs that do not exist
  // in the catalog. A single missing id in a batch delays the whole task by cooldownMs.
  it("waits the full cooldown to retry a 404 (current behavior)", async () => {
    scriptFetchOutcomes({ 1: [new HttpStatusError(404, "Not found"), new HttpStatusError(404, "Not found")] });

    const resultPromise = processInstantGamingTask(buildTask([1]), {});
    await vi.advanceTimersByTimeAsync(config.cooldownMs);
    const result = await resultPromise;

    expect(result).toEqual([]);
    expect(fetchOfferMock).toHaveBeenCalledTimes(2);
  });
});
