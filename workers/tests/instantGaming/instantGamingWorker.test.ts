import { beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import type { ConsumeMessage } from "amqplib";
import { createOrchestratorListener } from "../../src/utils/orchestratorListener.js";
import processInstantGamingTask from "../../src/instantGaming/processInstantGamingTask.js";
import { config, redis, testChannel } from "../mocks/config.js";
import { GameOffer } from "../../src/utils/types/entities/gameOffer.js";
import { eCurrency } from "../../src/utils/types/enums/eCurrency.js";
import { eVendor } from "../../src/utils/types/enums/eVendor.js";

vi.mock("../../src/utils/orchestratorListener.js", () => ({
  createOrchestratorListener: vi.fn(async () => undefined),
}));

vi.mock("../../src/instantGaming/processInstantGamingTask.js", () => ({
  default: vi.fn(),
}));

type MessageHandler = (msg: ConsumeMessage | null) => Promise<void>;

const listenerMock = vi.mocked(createOrchestratorListener);
const processTaskMock = vi.mocked(processInstantGamingTask);

let handleMessage: MessageHandler;
// Mocks are cleared before each test, so startup calls are captured once in beforeAll.
let startupListenerArgs: unknown[];
let startupPrefetchArgs: unknown[][];

const OFFER: GameOffer = {
  id: "offer-1",
  createdAt: "Mon, 26 Oct 2026 00:00:00 GMT",
  updatedAt: "Mon, 26 Oct 2026 00:00:00 GMT",
  gameId: "game-1",
  vendorsGameId: "7741",
  vendor: eVendor.InstatnGaming,
  vendorsUrl: "https://www.instant-gaming.com/7741-/",
  available: true,
  amount: 19.99,
  currency: eCurrency.EUR,
  offerName: "Cyberpunk 2077 - PC - Steam",
};

// Shape of a task published by the .NET backend (PascalCase).
const BACKEND_TASK = {
  TaskId: "task-1",
  CreatedAt: "2026-10-26T00:00:00Z",
  UpdateExisting: true,
  RedisResultKey: "ig:task-1",
  Currency: "USD",
  Proxy: "http://proxy.test:8080",
  VendorsIds: [7741, "7742"],
};

function messageWith(content: string): ConsumeMessage {
  return { content: Buffer.from(content), fields: {}, properties: {} } as unknown as ConsumeMessage;
}

describe("instantGamingWorker", () => {
  beforeAll(async () => {
    await import("../../src/instantGaming/instantGamingWorker.js");
    await vi.waitFor(() => expect(listenerMock).toHaveBeenCalled());
    startupListenerArgs = listenerMock.mock.calls[0];
    startupPrefetchArgs = [...testChannel.prefetch.mock.calls];
    handleMessage = listenerMock.mock.calls[0][3];
  });

  it("listens on the InstantGaming queues with prefetch 1", () => {
    expect(startupPrefetchArgs).toEqual([[1]]);
    expect(startupListenerArgs).toEqual([
      testChannel,
      config.instantGamingRequests,
      config.instantGamingResults,
      expect.any(Function),
    ]);
  });

  describe("message handling", () => {
    beforeEach(() => {
      processTaskMock.mockReset().mockResolvedValue([OFFER]);
    });

    it("ignores a null message", async () => {
      await handleMessage(null);

      expect(processTaskMock).not.toHaveBeenCalled();
      expect(testChannel.ack).not.toHaveBeenCalled();
      expect(testChannel.nack).not.toHaveBeenCalled();
    });

    it("rejects invalid JSON without requeueing it", async () => {
      const msg = messageWith("{not json");

      await handleMessage(msg);

      expect(testChannel.nack).toHaveBeenCalledWith(msg, false, false);
      expect(processTaskMock).not.toHaveBeenCalled();
      expect(testChannel.ack).not.toHaveBeenCalled();
    });

    it("normalizes the backend task before processing it", async () => {
      const msg = messageWith(JSON.stringify(BACKEND_TASK));

      await handleMessage(msg);

      expect(processTaskMock).toHaveBeenCalledWith(
        {
          taskId: "task-1",
          createdAt: "2026-10-26T00:00:00Z",
          updateExistingDeals: true,
          redisResultKey: "ig:task-1",
          currency: "USD",
          proxy: "http://proxy.test:8080",
          vendorsIds: [7741, 7742],
        },
        msg,
      );
    });

    it("stores offers in Redis, notifies the backend and acks the message", async () => {
      const secondOffer = { ...OFFER, id: "offer-2", vendorsGameId: "7742" };
      processTaskMock.mockResolvedValue([OFFER, secondOffer]);
      const msg = messageWith(JSON.stringify(BACKEND_TASK));

      await handleMessage(msg);

      expect(redis.rpush).toHaveBeenCalledWith("ig:task-1", JSON.stringify(OFFER), JSON.stringify(secondOffer));
      expect(testChannel.sendToQueue).toHaveBeenCalledWith(
        config.instantGamingResults,
        expect.any(Buffer),
        { persistent: true },
      );
      const notification = JSON.parse((testChannel.sendToQueue.mock.calls[0] as unknown[])[1]!.toString());
      expect(notification).toEqual({ taskId: "task-1", redisResultKey: "ig:task-1" });
      expect(testChannel.ack).toHaveBeenCalledWith(msg);
      expect(testChannel.nack).not.toHaveBeenCalled();
    });

    it("writes to Redis before notifying and acks last", async () => {
      await handleMessage(messageWith(JSON.stringify(BACKEND_TASK)));

      const rpushOrder = redis.rpush.mock.invocationCallOrder[0];
      const notifyOrder = testChannel.sendToQueue.mock.invocationCallOrder[0];
      const ackOrder = testChannel.ack.mock.invocationCallOrder[0];
      expect(rpushOrder).toBeLessThan(notifyOrder);
      expect(notifyOrder).toBeLessThan(ackOrder);
    });

    it("acks without writing to Redis or notifying when no offers were found", async () => {
      processTaskMock.mockResolvedValue([]);
      const msg = messageWith(JSON.stringify(BACKEND_TASK));

      await handleMessage(msg);

      expect(testChannel.ack).toHaveBeenCalledWith(msg);
      expect(redis.rpush).not.toHaveBeenCalled();
      expect(testChannel.sendToQueue).not.toHaveBeenCalled();
    });

    // FIXME: if processing or the Redis write throws, the message is neither acked nor nacked and stays unacked
    // on the channel (prefetch 1 blocks the worker). amqplib does not await the handler, so the rejection is unhandled.
    it("leaves the message unacknowledged when processing throws (current behavior)", async () => {
      processTaskMock.mockRejectedValue(new Error("boom"));

      await expect(handleMessage(messageWith(JSON.stringify(BACKEND_TASK)))).rejects.toThrow("boom");

      expect(testChannel.ack).not.toHaveBeenCalled();
      expect(testChannel.nack).not.toHaveBeenCalled();
    });

    it("leaves the message unacknowledged when the Redis write fails (current behavior)", async () => {
      redis.rpush.mockRejectedValueOnce(new Error("Redis down"));

      await expect(handleMessage(messageWith(JSON.stringify(BACKEND_TASK)))).rejects.toThrow("Redis down");

      expect(testChannel.sendToQueue).not.toHaveBeenCalled();
      expect(testChannel.ack).not.toHaveBeenCalled();
      expect(testChannel.nack).not.toHaveBeenCalled();
    });
  });
});
