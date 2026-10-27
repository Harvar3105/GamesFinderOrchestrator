export type InstantGamingPageOptions = {
  title?: string | null;
  price?: string | null;
  noStock?: boolean;
  canonicalUrl?: string | null;
  steamMediaIds?: number[];
};

/** Builds a minimal InstantGaming product page with the elements the scraper reads. */
export function buildInstantGamingPage({
  title = "Cyberpunk 2077 - PC - Steam",
  price = "19.99€",
  noStock = false,
  canonicalUrl = "https://www.instant-gaming.com/en/7741-buy-cyberpunk-2077-pc-steam/",
  steamMediaIds = [1091500],
}: InstantGamingPageOptions = {}): string {
  const canonical = canonicalUrl ? `<link rel="canonical" href="${canonicalUrl}">` : "";
  const titleElement = title !== null ? `<h1 class="game-title">${title}</h1>` : "";
  const priceElement = price !== null ? `<div class="total">${price}</div>` : "";
  const noStockElement = noStock ? `<div class="noStock">Out of stock</div>` : "";
  const media = steamMediaIds
    .map(id => `<video src="https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/${id}/extras/trailer.webm"></video>`)
    .join("\n");

  return `<!DOCTYPE html>
<html>
  <head>
    <title>InstantGaming</title>
    ${canonical}
  </head>
  <body>
    ${titleElement}
    <div class="amount">${priceElement}${noStockElement}</div>
    <div class="media">${media}</div>
  </body>
</html>`;
}
