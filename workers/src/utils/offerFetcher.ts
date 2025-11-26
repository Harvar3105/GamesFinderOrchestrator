import { GameOffer } from "./types/entities/gameOffer.js";

export async function fetchJson(url: string): Promise<any | null> {
  const res = await fetch(url);
  if (!res.ok) return null;
  const data = await res.json();
  if (data === undefined || data.error) return null;
  return data;
}