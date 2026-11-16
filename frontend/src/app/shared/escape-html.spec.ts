import { escapeHtml } from './escape-html';

describe('escapeHtml', () => {
  it('escapes every HTML-significant character', () => {
    expect(escapeHtml(`<a href="x" title='y'>&</a>`)).toBe('&lt;a href=&quot;x&quot; title=&#39;y&#39;&gt;&amp;&lt;/a&gt;');
  });

  it('leaves plain text alone', () => {
    expect(escapeHtml('Ann Lee')).toBe('Ann Lee');
  });
});
