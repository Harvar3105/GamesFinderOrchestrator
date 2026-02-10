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

//TODO: Pass object with named params instead of multiple params
export async function fetchJson(url: string, proxy?: string, method?: string): Promise<any | HttpStatusError | null> {
  const options = proxy ? {
    method: method ?? 'GET',
    agent: new HttpsProxyAgent(proxy),
    timeout: config.backendTimeoutMs
  }
  : {
    method: method ?? 'GET',
    timeout: config.backendTimeoutMs
  };
  try {
    const res = await fetch(url, options);

    if (!res.ok && !res.status.toString().startsWith('3')) {
      const body = await res.text().catch(() => undefined);
      logger.warn(`Received ${res.status} from ${url}, throwing HttpStatusError`);
      throw new HttpStatusError(res.status, `HTTP ${res.status} ${res.statusText}`, body);
    }

    try {
      return await res.json();
    } catch (parseErr) {
      logger.error(`❌Error parsing JSON from ${url}`, parseErr);
      return null;
    }
  } catch (err) {
    if (err instanceof HttpStatusError) throw err;
    logger.error(`❌Error fetching JSON from ${url}`, err);
    return null;
  }
}

export async function fetchHTML(url: string, proxy?: string): Promise<string | HttpStatusError | null> {
  const options = proxy ? {
    method: 'GET',
    agent: new HttpsProxyAgent(proxy),
    timeout: config.backendTimeoutMs
  }
  : {
    method: 'GET',
    timeout: config.backendTimeoutMs
  };

  try {
    const res = await fetch(url, options);
    logger.info(`Fetched HTML from ${url} with status: ${res.status}`);

    if (!res.ok && !res.status.toString().startsWith('3')) {
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
  const dom = new JSDOM(html);
  return dom.window.document;
}