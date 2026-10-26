import { HttpsProxyAgent } from "https-proxy-agent";
import logger from "./logger.js";
import { config } from "./config.js";
import { JSDOM } from 'jsdom';
import fetch, { AbortError, FetchError } from 'node-fetch';

export class HttpStatusError extends Error {
  status: number;
  body?: string;
  constructor(status: number, message: string, body?: string) {
    super(message);
    this.name = 'HttpStatusError';
    this.status = status;
    this.body = body;
  }
}

export class WrongDataError extends Error {
  body?: string;
  constructor(message: string, body?: string){
    super(message);
    this.name = 'WrongDataError';
    this.body = body;
  }
}

export type FetchParams = {
  url: string;
  proxy?: string;
  method?: string;
  timeoutMS?: number;
};

export const FetchFailureStatus = {
  Timeout: 504,
  NetworkError: 503,
} as const;

const timeoutSystemErrorCodes = new Set(['ETIMEDOUT', 'ESOCKETTIMEDOUT', 'UND_ERR_CONNECT_TIMEOUT']);

function buildRequestOptions(timeoutMS: number, method: string = 'GET', proxy?: string) {
  return {
    method,
    redirect: 'follow' as const,
    signal: AbortSignal.timeout(timeoutMS),
    ...(proxy && { agent: new HttpsProxyAgent(proxy) }),
  };
}

function toHttpStatusError(err: unknown, url: string, timeoutMS: number): HttpStatusError {
  if (err instanceof HttpStatusError) return err;

  const isTimeout =
    err instanceof AbortError ||
    (err instanceof FetchError && timeoutSystemErrorCodes.has(err.code ?? ''));
  if (isTimeout) {
    return new HttpStatusError(FetchFailureStatus.Timeout, `Request to ${url} timed out after ${timeoutMS}ms`);
  }

  const reason = err instanceof Error ? err.message : String(err);
  return new HttpStatusError(FetchFailureStatus.NetworkError, `Request to ${url} failed: ${reason}`);
}

export async function fetchJson(params: FetchParams): Promise<any | HttpStatusError | WrongDataError> {
  const timeoutMS = params.timeoutMS ?? config.backendTimeoutMs;
  try {
    const res = await fetch(params.url, buildRequestOptions(timeoutMS, params.method, params.proxy));

    if (!res.ok) {
      const body = await res.text().catch(() => undefined);
      const error = new HttpStatusError(res.status, `HTTP ${res.status} ${res.statusText}`, body);
      logger.warn(`Received ${res.status} from ${params.url}`, error);
      return error;
    }

    const body = await res.text();
    try {
      return JSON.parse(body);
    } catch (parseErr) {
      logger.error(`❌Error parsing JSON from ${params.url}`, parseErr);
      return new WrongDataError(`Invalid JSON received from ${params.url}`, body);
    }
  } catch (err) {
    const error = toHttpStatusError(err, params.url, timeoutMS);
    logger.error(`❌Error fetching JSON from ${params.url}: ${error.status} ${error.message}`);
    return error;
  }
}

export async function fetchHTML(url: string, proxy?: string): Promise<string> {
  const timeoutMS = config.backendTimeoutMs;
  let html: string;
  try {
    const res = await fetch(url, buildRequestOptions(timeoutMS, 'GET', proxy));

    if (!res.ok) {
      const body = await res.text().catch(() => undefined);
      throw new HttpStatusError(res.status, `Could not fetch HTML from ${url}.`, body);
    }
    html = await res.text();
  } catch (err) {
    const error = toHttpStatusError(err, url, timeoutMS);
    logger.error(`❌Error fetching HTML from ${url}: ${error.status} ${error.message}`);
    throw error;
  }

  if (html.trim().length === 0) throw new WrongDataError("Recieved html is blank!");
  return html;
}

export function parseHtmlToDocument(html: string): Document {
  const dom = new JSDOM(html, {
    contentType: 'text/html',
    pretendToBeVisual: true
  });
  return dom.window.document;
}
