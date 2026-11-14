// Merges Chromium V8 coverage captured during e2e runs into one Cobertura report for src/**/*.ts.
//
// Usage: npm run coverage:e2e -- <rawDir> <outDir>
//   rawDir  every *.json is an array of { url, scriptId, source, functions, sourceMap? } entries
//   outDir  receives cobertura-coverage.xml
import { readdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import CoverageReport from 'monocart-coverage-reports';

// Keep in sync with coverageExclude in angular.json (unit coverage).
const EXCLUDED_SOURCES = [/\.spec\.ts$/, /(^|\/)src\/main\.ts$/, /(^|\/)src\/environments\//, /(^|\/)src\/app\/app\.config\.ts$/];

// Scripts that are never app code: dependencies, Vite client/runtime, prebundled deps.
// Top-level /chunk-*.js files are kept: they hold the lazy-loaded route components.
const VENDOR_URL = /node_modules|\/@vite\/|\/@fs\/|\/@id\/|\/\.vite\/|\/\.angular\//i;

/** Normalizes a source path from a source map (may be `../../src/x.ts`, `/src/x.ts`, `file:///…/src/x.ts`). */
export function toSrcPath(source) {
  const normalized = source.replaceAll('\\', '/').split('?')[0];
  const match = /(?:^|\/)(src\/.+)$/.exec(normalized);

  return match ? match[1] : null;
}

export function isAppSource(source) {
  const srcPath = toSrcPath(source);
  if (!srcPath || !srcPath.endsWith('.ts') || source.includes('node_modules/')) return false;

  return !EXCLUDED_SOURCES.some((pattern) => pattern.test(srcPath));
}

export function isAppEntry(entry) {
  return Boolean(entry.url) && !VENDOR_URL.test(entry.url);
}

export async function buildReport(rawDir, outDir) {
  const names = (await readdir(rawDir)).filter((name) => name.endsWith('.json')).sort();
  const report = new CoverageReport({
    name: 'ContactHub e2e coverage',
    outputDir: outDir,
    reports: [['cobertura', { file: 'cobertura-coverage.xml' }]],
    logging: 'error',
    cleanCache: true,
    entryFilter: isAppEntry,
    sourceFilter: isAppSource,
    sourcePath: (filePath) => toSrcPath(filePath) ?? filePath,
  });
  let entries = 0;
  // Merge file by file so every capture adds to the same report.
  for (const name of names) {
    const text = await readFile(path.join(rawDir, name), 'utf8');
    const fileEntries = JSON.parse(text);
    entries += fileEntries.length;
    await report.add(fileEntries);
  }
  await report.generate();
  // monocart joins paths with the OS separator; Cobertura consumers expect forward slashes.
  const xmlPath = path.join(outDir, 'cobertura-coverage.xml');
  const xml = await readFile(xmlPath, 'utf8');
  const fixed = xml.replace(/filename="([^"]*)"/g, (_, name) => `filename="${name.replaceAll('\\', '/')}"`);
  await writeFile(xmlPath, fixed);

  return { files: names.length, entries };
}

async function main() {
  const [rawDir, outDir] = process.argv.slice(2);
  if (!rawDir || !outDir) {
    console.error('Usage: npm run coverage:e2e -- <rawDir> <outDir>');
    process.exit(2);
  }
  const { files, entries } = await buildReport(path.resolve(rawDir), path.resolve(outDir));
  console.log(`Merged ${entries} V8 entries from ${files} file(s) into ${path.join(outDir, 'cobertura-coverage.xml')}`);
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  await main();
}
