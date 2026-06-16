const { readFileSync, writeFileSync } = require('fs');
const { resolve } = require('path');

const root = resolve(__dirname, '..');
const pkg = JSON.parse(readFileSync(resolve(root, 'package.json'), 'utf8'));
const version = pkg.version;
const buildTime = new Date().toISOString();

writeFileSync(
  resolve(root, 'public/version.json'),
  JSON.stringify({ version, buildTime }, null, 2) + '\n'
);

writeFileSync(
  resolve(root, 'src/app/version.ts'),
  `export const APP_VERSION = '${version}';\n`
);

console.log(`[set-version] ${version} (${buildTime})`);
