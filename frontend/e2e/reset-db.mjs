import { rmSync } from 'node:fs';
import { join } from 'node:path';

const dir = join(process.cwd(), '..', 'backend', 'src', 'Api', 'bin', 'Debug', 'net10.0', 'database');
for (const suffix of ['', '-shm', '-wal']) {
  rmSync(join(dir, `contacthub_e2e.db${suffix}`), { force: true });
}
