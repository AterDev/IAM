export interface CsvColumn<T> {
  header: string;
  value: (row: T) => unknown;
}

export function toCsv<T>(rows: readonly T[], columns: readonly CsvColumn<T>[]): string {
  const lines = [
    columns.map(column => escapeCsvCell(column.header)).join(','),
    ...rows.map(row => columns.map(column => escapeCsvCell(column.value(row))).join(',')),
  ];

  return `\uFEFF${lines.join('\r\n')}\r\n`;
}

function escapeCsvCell(value: unknown): string {
  let text = value === null || value === undefined ? '' : String(value);

  // Prefix formula-like values so spreadsheet applications treat them as text.
  if (/^[\t\r\n ]*[=+\-@]/.test(text)) {
    text = `'${text}`;
  }

  if (/[",\r\n]/.test(text)) {
    return `"${text.replace(/"/g, '""')}"`;
  }

  return text;
}
