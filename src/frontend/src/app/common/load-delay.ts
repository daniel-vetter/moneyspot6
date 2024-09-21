export async function minDelay<T>(promise: Promise<T>): Promise<T> {
    let timer = new Promise<void>((resolve) => {
        setTimeout(() => { resolve(); }, 300)
    });

    const [_, result] = await Promise.all([timer, promise]);
    return result;
}