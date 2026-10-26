import { HttpsProxyAgent } from "https-proxy-agent";
import logger from "./logger.js";
import { config } from "./config.js";
import { JSDOM } from 'jsdom';

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
// FIXME: Global (native) fetch ignores the `agent` option, so the proxy is never used.
// Use undici's ProxyAgent via `dispatcher`, or import node-fetch (already a dependency).
export async function fetchJson(params: FetchParams): Promise<any | HttpStatusError> {
  const options = params.proxy ? {
    method: params.method ?? 'GET',
    agent: new HttpsProxyAgent(params.proxy),
    signal: AbortSignal.timeout(params.timeoutMS ?? config.backendTimeoutMs),
    redirect: 'follow' as const
  }
  : {
    method: params.method ?? 'GET',
    signal: AbortSignal.timeout(params.timeoutMS ?? config.backendTimeoutMs),
    redirect: 'follow' as const
  };
  try {
    const res = await fetch(params.url, options);

    if (!res.ok) {
      const body = await res.text().catch(() => undefined);
      const error = new HttpStatusError(res.status, `HTTP ${res.status} ${res.statusText}`, body);
      logger.warn(`Received ${res.status} from ${params.url}`, error);
      return error;
    }

    try {
      return await res.json();
    } catch (parseErr) {
      const error = new HttpStatusError(-2, `Failed to read or parse JSON from ${params.url}`);
      logger.error(`❌Error reading/parsing JSON from ${params.url}`, parseErr);
      return error;
    }
  } catch (err) {
    const error = new HttpStatusError(-1, `Failed to fetch JSON from ${params.url}`);
    logger.error(`❌Error fetching JSON from ${params.url}`, err);
    return error;
  }
}

// FIXME: Native fetch ignores both `agent` and `timeout`: no proxy is applied and requests
// can hang indefinitely. Use `signal: AbortSignal.timeout(...)` as in fetchJson.
export async function fetchHTML(url: string, proxy?: string): Promise<string> {
  const options = proxy ? {
    method: 'GET',
    agent: new HttpsProxyAgent(proxy),
    timeout: config.backendTimeoutMs,
    redirect: 'follow' as const
  }
  : {
    method: 'GET',
    timeout: config.backendTimeoutMs,
    redirect: 'follow' as const
  };

  const res = await fetch(url, options);

  if (!res.ok) {
    const body = await res.text().catch(() => undefined);
    logger.error(`❌Received ${res.status} from ${url}.`, body);
    throw new HttpStatusError(res.status, `Could not fetch HTML from ${url}.`, body);
  }
  const html = await res.text();
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
