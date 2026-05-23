import sharp from 'sharp';
import { writeFileSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));

const makeSvg = (size, radius) => Buffer.from(`
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${size} ${size}" width="${size}" height="${size}">
  <rect width="${size}" height="${size}" rx="${radius}" fill="#7c5bf6"/>
  <text x="${size / 2}" y="${size * 0.72}" font-family="Arial, sans-serif"
        font-size="${size * 0.56}" font-weight="bold" text-anchor="middle" fill="white">N</text>
</svg>`);

const out = (name) => join(__dirname, 'public', 'icons', name);

await sharp(makeSvg(192, 32)).png().toFile(out('icon-192.png'));
console.log('✓ icon-192.png');

await sharp(makeSvg(512, 80)).png().toFile(out('icon-512.png'));
console.log('✓ icon-512.png');
