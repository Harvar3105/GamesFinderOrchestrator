import { HttpsProxyAgent } from "https-proxy-agent";
import logger from "./logger.js";
import { config } from "./config.js";

export async function fetchJson(url: string, proxy?: string): Promise<any | null> {
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
    if (!res.ok) return null;
    const data = await res.json();
    if (data === undefined || data.error) return null;
    return data;
  } catch (err) {
    logger.error(`❌Error fetching JSON from ${url}:`, err);
    return null;
  }
  
}