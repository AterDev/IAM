import { toCsv } from './csv';

describe('toCsv', () => {
  it('includes a UTF-8 BOM and escapes commas, quotes, and newlines', () => {
    const csv = toCsv(
      [{ category: 'Authentication', event: 'Login,Success', subjectId: 'user"1\nnext' }],
      [
        { header: 'Category', value: row => row.category },
        { header: 'Event', value: row => row.event },
        { header: 'Subject', value: row => row.subjectId },
      ],
    );

    expect(csv).toBe('\uFEFFCategory,Event,Subject\r\nAuthentication,"Login,Success","user""1\nnext"\r\n');
  });

  it('prefixes formula-like values to prevent spreadsheet injection', () => {
    const csv = toCsv(
      [{ subjectId: '=HYPERLINK("https://example.com")' }],
      [{ header: 'Subject', value: row => row.subjectId }],
    );

    expect(csv).toContain("'=HYPERLINK(\"\"https://example.com\"\")");
  });
});
