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
export type FetchParams = {
  url: string;
  proxy?: string;
  method?: string;
  timeoutMS?: number;
};
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

export async function fetchHTML(url: string, proxy?: string): Promise<string | null> {
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

  try {
    const res = await fetch(url, options);

    if (!res.ok) {
      const body = await res.text().catch(() => undefined);
      logger.warn(`Received ${res.status} from ${url}, throwing HttpStatusError`);
      throw new HttpStatusError(res.status, `HTTP ${res.status} ${res.statusText}`, body);
    }
    return await res.text();
  } catch (err) {
    logger.error(`❌Error fetching HTML from ${url}`, err);
    return null;
  }
}

export function parseHtmlToDocument(html: string): Document {
  const dom = new JSDOM(html, {
    contentType: 'text/html',
    pretendToBeVisual: true
  });
  return dom.window.document;
}
