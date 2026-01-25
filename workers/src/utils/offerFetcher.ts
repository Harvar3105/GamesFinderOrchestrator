import { HttpsProxyAgent } from "https-proxy-agent";
import logger from "./logger.js";
import { config } from "./config.js";
import { JSDOM } from 'jsdom';

//TODO: Pass object with named params instead of multiple params
export async function fetchJson(url: string, proxy?: string, method?: string): Promise<any | null> {
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
    logger.info(`Options are: \t${JSON.stringify(options)}`);
    const res = await fetch(url, options);
    if (!res.ok) return null;
    const data = await res.json();
    if (data === undefined || data.error) return null;
    return data;
  } catch (err) {
    logger.error(`❌Error fetching JSON from ${url}`, err);
    return null;
  }
}

export async function fetchHTML(url: string, proxy?: string): Promise<string | null> {
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

    if (!res.ok) return null;
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