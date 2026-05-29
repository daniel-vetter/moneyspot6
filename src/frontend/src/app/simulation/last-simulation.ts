const STORAGE_KEY = 'lastOpenedSimulationId';

/** Remembers which simulation was opened last, so the simulation page can reopen it. */
export function rememberSimulationId(id: number): void {
    localStorage.setItem(STORAGE_KEY, String(id));
}

/**
 * Picks the simulation to open for the bare /simulation route: the most recently opened one
 * if it still exists, otherwise the one with the highest id, or undefined when there are none.
 */
export function pickLatestSimulationId(availableIds: readonly number[]): number | undefined {
    if (availableIds.length === 0) {
        return undefined;
    }

    const stored = localStorage.getItem(STORAGE_KEY);
    const storedId = stored !== null ? Number(stored) : undefined;
    if (storedId !== undefined && availableIds.includes(storedId)) {
        return storedId;
    }

    return Math.max(...availableIds);
}
