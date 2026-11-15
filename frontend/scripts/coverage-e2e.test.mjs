import { mkdir, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import { transform } from 'esbuild';
import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { buildReport, isAppSource, toSrcPath } from './coverage-e2e.mjs';

const TS = 'export function used() {\n  return 1;\n}\nexport function unused() {\n  return 2;\n}\n';

/** Builds a V8 entry for compiled TS: `used` ran, `unused` did not. */
async function entryFor(sourcefile, url, usedCount) {
  const { code, map } = await transform(TS, { loader: 'ts', sourcefile, sourcemap: 'external' });
  const unusedStart = code.indexOf('function unused');

  return {
    url,
    scriptId: '1',
    source: code,
    sourceMap: JSON.parse(map),
    functions: [
      {
        functionName: '',
        isBlockCoverage: true,
        ranges: [{ startOffset: 0, endOffset: code.length, count: 1 }],
      },
      {
        functionName: 'unused',
        isBlockCoverage: true,
        ranges: [{ startOffset: unusedStart, endOffset: code.length, count: usedCount }],
      },
    ],
  };
}

describe('coverage:e2e', () => {
  let dir;

  beforeAll(async () => {
    dir = await mkdtemp(path.join(os.tmpdir(), 'coverage-e2e-'));
    await mkdir(path.join(dir, 'raw'));
    const app = await entryFor('../../src/app/widget.ts', 'http://127.0.0.1:4700/widget.js', 0);
    const second = await entryFor('../../src/app/widget.ts', 'http://127.0.0.1:4700/widget.js', 1);
    const main = await entryFor('../../src/main.ts', 'http://127.0.0.1:4700/main.js', 1);
    const vendor = await entryFor('../../node_modules/lib/index.ts', 'http://127.0.0.1:4700/node_modules/.vite/deps/lib.js', 1);
    await writeFile(path.join(dir, 'raw', 'a.json'), JSON.stringify([app, main, vendor]));
    await writeFile(path.join(dir, 'raw', 'b.json'), JSON.stringify([second]));
    await writeFile(path.join(dir, 'raw', 'notes.txt'), 'ignored');
  });

  afterAll(async () => {
    await rm(dir, { recursive: true, force: true });
  });

  it('normalizes and filters source paths', () => {
    expect(toSrcPath('../../src/app/a.ts')).toBe('src/app/a.ts');
    expect(toSrcPath('file:///C:/x/frontend/src/app/a.ts?v=1')).toBe('src/app/a.ts');
    expect(toSrcPath('node_modules/x/index.js')).toBeNull();
    expect(isAppSource('src/app/a.ts')).toBe(true);
    expect(isAppSource('src/app/a.spec.ts')).toBe(false);
    expect(isAppSource('src/main.ts')).toBe(false);
    expect(isAppSource('src/environments/environment.ts')).toBe(false);
    expect(isAppSource('src/app/app.config.ts')).toBe(false);
    expect(isAppSource('../../node_modules/x/src/a.ts')).toBe(false);
  });

  it('writes a merged cobertura report limited to src/**/*.ts', async () => {
    const outDir = path.join(dir, 'out');
    const { files, entries } = await buildReport(path.join(dir, 'raw'), outDir);
    const xml = await readFile(path.join(outDir, 'cobertura-coverage.xml'), 'utf8');

    expect(files).toBe(2);
    expect(entries).toBe(4);
    expect(xml).toContain('filename="src/app/widget.ts"');
    expect(xml).not.toContain('main.ts');
    expect(xml).not.toContain('node_modules');
    // `unused` ran only in the second capture; merging makes every line covered.
    expect(xml).not.toMatch(/<line number="\d+" hits="0"/);
  });

  it('reports lines that no capture executed', async () => {
    const outDir = path.join(dir, 'out-single');
    const only = path.join(dir, 'raw-single');
    await mkdir(only);
    const entry = await entryFor('../../src/app/widget.ts', 'http://127.0.0.1:4700/widget.js', 0);
    await writeFile(path.join(only, 'one.json'), JSON.stringify([entry]));
    await buildReport(only, outDir);
    const xml = await readFile(path.join(outDir, 'cobertura-coverage.xml'), 'utf8');

    expect(xml).toMatch(/<line number="5" hits="0"/);
    expect(xml).toMatch(/<line number="2" hits="1"/);
  });
});
