import { fetchJson, HttpStatusError } from "../utils/offerFetcher.js";

export type GameStoreMetaData = {
  tags: string[];
  genres: string[];
  positiveReviews: number;
  negativeReviews: number;
  positiveReviewsPercent: number;
}

// This method should not be called more than once per second due to steamspy api limitations, otherwise it will return 403 error. We can not use proxies here because steamspy blocks them as well.
export async function fetchGameStoreMetadata(id: number): Promise<null | HttpStatusError | GameStoreMetaData> {
  const url = `https://steamspy.com/api.php?request=appdetails&appid=${id}`;

  let data = null;
  try {
    data = await fetchJson(url);
    if (!data.name) return null;
  } catch (err) {
    if (err instanceof HttpStatusError) return err;
    return null;
  }

  if (!data.tags || !data.genre) return null;

  const genres = data["genre"]?.split(", ") || [];
  const tags = Object.keys(data["tags"]) || [];
  const posRevs = data["positive"] || 0;
  const negRevs = data["negative"] || 0;
  const posRevPercent = posRevs + negRevs === 0 ? 0 : posRevs * 100 / (posRevs + negRevs);

  return { tags, genres, positiveReviews: posRevs, negativeReviews: negRevs, positiveReviewsPercent: posRevPercent } as GameStoreMetaData;
}
