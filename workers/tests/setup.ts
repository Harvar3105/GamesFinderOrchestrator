import { vi } from "vitest";

// config.ts connects to RabbitMQ and Redis on import, so every test uses an in-memory replacement.
vi.mock("../src/utils/config.js", () => import("./mocks/config.js"));
vi.mock("../src/utils/logger.js", () => import("./mocks/logger.js"));
