import { vi } from "vitest";

// Mirrors winston's default (npm) levels.
const logger = {
  error: vi.fn(),
  warn: vi.fn(),
  info: vi.fn(),
  http: vi.fn(),
  verbose: vi.fn(),
  debug: vi.fn(),
  silly: vi.fn(),
};

export default logger;
