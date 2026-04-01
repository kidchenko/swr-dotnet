#!/usr/bin/env node
/**
 * Post-processing script for xmldocmd-generated markdown files.
 * Adds Starlight-required `title:` frontmatter if the file has none.
 * Extracts title from the first H1 heading in the file.
 */

import { readdir, readFile, writeFile, stat } from 'fs/promises';
import { join, relative } from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';

const __dirname = dirname(fileURLToPath(import.meta.url));
const referenceDir = join(__dirname, '../src/content/docs/reference');

async function getMarkdownFiles(dir) {
  const entries = await readdir(dir, { withFileTypes: true });
  const files = [];
  for (const entry of entries) {
    const fullPath = join(dir, entry.name);
    if (entry.isDirectory()) {
      const subFiles = await getMarkdownFiles(fullPath);
      files.push(...subFiles);
    } else if (entry.name.endsWith('.md')) {
      files.push(fullPath);
    }
  }
  return files;
}

async function processFile(filePath) {
  const content = await readFile(filePath, 'utf8');

  // Skip if already has frontmatter
  if (content.startsWith('---')) {
    return false;
  }

  // Extract first H1 heading as title
  const h1Match = content.match(/^# (.+)$/m);
  if (!h1Match) {
    return false;
  }

  const title = h1Match[1].trim();
  const frontmatter = `---\ntitle: "${title.replace(/"/g, '\\"')}"\n---\n\n`;
  await writeFile(filePath, frontmatter + content, 'utf8');
  return true;
}

async function main() {
  const files = await getMarkdownFiles(referenceDir);
  let processed = 0;
  let skipped = 0;

  for (const file of files) {
    const updated = await processFile(file);
    if (updated) {
      console.log(`  added frontmatter: ${relative(referenceDir, file)}`);
      processed++;
    } else {
      skipped++;
    }
  }

  console.log(`\nProcessed ${processed} files, skipped ${skipped} (already had frontmatter)`);
}

main().catch(err => {
  console.error('Error:', err);
  process.exit(1);
});
