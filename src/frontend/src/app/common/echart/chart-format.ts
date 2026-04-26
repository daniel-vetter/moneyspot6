const monthsDe = ['Jan', 'Feb', 'Mrz', 'Apr', 'Mai', 'Jun', 'Jul', 'Aug', 'Sep', 'Okt', 'Nov', 'Dez'];

export function formatEur(n: number): string {
    return `${n.toLocaleString('de-DE', {minimumFractionDigits: 2, maximumFractionDigits: 2})} €`;
}

export function formatDateDe(timestamp: number): string {
    const d = new Date(timestamp);
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${pad(d.getDate())}.${pad(d.getMonth() + 1)}.${d.getFullYear()}`;
}

export function formatTimeAxisLabelDe(value: number): string {
    const d = new Date(value);
    if (d.getMonth() === 0 && d.getDate() === 1) return d.getFullYear().toString();
    if (d.getDate() === 1) return monthsDe[d.getMonth()];
    return `${d.getDate()}. ${monthsDe[d.getMonth()]}`;
}
