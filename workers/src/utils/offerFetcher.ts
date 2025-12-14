import { HttpsProxyAgent } from "https-proxy-agent";

export async function fetchJson(url: string, proxy?: string): Promise<any | null> {
  const options = proxy ? {
    method: 'GET',
    agent: new HttpsProxyAgent(proxy)
  }
  : {
    method: 'GET'
  };
  const res = await fetch(url, options);
  if (!res.ok) return null;
  const data = await res.json();
  if (data === undefined || data.error) return null;
  return data;
}