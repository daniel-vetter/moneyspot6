---
name: update-screenshots
description: Refresh the README screenshots in docs/screenshots/. Captures dashboard, history, categories, stock-history (1d range), cashflow and transactions in light theme at 1920x1080. Use when the user asks to "update screenshots", "refresh README screenshots" or similar.
---

# Update README screenshots

Goal: regenerate the PNGs in `docs/screenshots/` so the README stays current with UI changes. Output paths are fixed; only the content of the PNGs changes.

## Preconditions (always check first)

1. **Dev server running**. Ask the user where it's reachable if you don't already know — usually `http://localhost:4200`. If `mcp__playwright__browser_navigate` returns the login redirect or fails, ask the user to start the AppHost (`dotnet run --project src/backend/MoneySpot6.AppHost`).
2. **Demo seed loaded** so charts have data. Dashboard should show a non-zero Gesamt value and the `iShares Core MSCI World UCITS ETF` row in Depot. If empty, ask the user to reseed.
3. **Light theme** must be active. Check after the first navigation — if you see a dark background, click the sun icon in the bottom-left of the sidebar (the first of three theme buttons; the others are neon and dark).

## Procedure

1. Resize viewport to **1920×1080** (`mcp__playwright__browser_resize`).
2. Navigate to the dev server's base URL once. Verify the demo seed is loaded and switch to light theme if needed.
3. For each entry in the table below:
   a. Click the matching menu link in the sidebar to navigate.
   b. Apply any per-page action listed in the Notes column.
   c. Take a viewport screenshot (not fullPage) to the output path.
4. Close the browser.
5. Verify all six files exist at 1920×1080 (Python one-liner reading PNG header is fine).
6. Report back: which files changed, briefly. Do not commit unless the user asks.

## Pages

| Sidebar link    | Output                                  | Notes                                   |
|-----------------|-----------------------------------------|-----------------------------------------|
| Dashboard       | `docs/screenshots/dashboard.png`        | —                                       |
| Verlauf         | `docs/screenshots/history.png`          | —                                       |
| Kategorien      | `docs/screenshots/categories.png`       | —                                       |
| Kursentwicklung | `docs/screenshots/stock-history.png`    | Click `1d` preset before screenshot     |
| Cashflow        | `docs/screenshots/cashflow.png`         | —                                       |
| Transaktionen   | `docs/screenshots/transactions.png`     | —                                       |

## Notes

- Page sizes are tuned to 1920×1080 with the existing component SCSS (chart heights use `calc(100vh - X)`). If you change the viewport, charts re-flow but most pages will still look fine; only categories may clip labels at significantly different heights.
- The README references these paths in a 3×2 markdown table — order doesn't matter for the markdown, but keep filenames stable.
- If a new chart-heavy page is added later, append a row above and update the README similarly.
- The `.playwright-mcp/` directory is gitignored; nothing there ends up in the repo.
