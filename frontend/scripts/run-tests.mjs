#!/usr/bin/env node

import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import ts from 'typescript';

const projectRoot = path.resolve(fileURLToPath(new URL('..', import.meta.url)));
const sourceRoots = ['src', 'tests'];
const tempRoot = fs.mkdtempSync(path.join(projectRoot, '.test-build-'));

function collectFiles(dir) {
  const entries = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const fullPath = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      entries.push(...collectFiles(fullPath));
    } else if (/\.(ts|tsx)$/.test(entry.name)) {
      entries.push(fullPath);
    }
  }
  return entries;
}

function collectJavaScriptFiles(dir) {
  const entries = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const fullPath = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      entries.push(...collectJavaScriptFiles(fullPath));
    } else if (entry.name.endsWith('.js')) {
      entries.push(fullPath);
    }
  }
  return entries;
}

function rewriteRelativeSpecifiers(code) {
  const withJsExtension = (specifier) => {
    if (path.extname(specifier)) {
      return specifier;
    }

    return `${specifier}.js`;
  };

  return code
    .replace(/(from\s+['"])(\.{1,2}\/[^'"]+)(['"])/g, (_match, prefix, specifier, suffix) => {
      return `${prefix}${withJsExtension(specifier)}${suffix}`;
    })
    .replace(
      /(export\s+[^'"]+\s+from\s+['"])(\.{1,2}\/[^'"]+)(['"])/g,
      (_match, prefix, specifier, suffix) => {
        return `${prefix}${withJsExtension(specifier)}${suffix}`;
      }
    )
    .replace(
      /(import\s*\(\s*['"])(\.{1,2}\/[^'"]+)(['"]\s*\))/g,
      (_match, prefix, specifier, suffix) => {
        return `${prefix}${withJsExtension(specifier)}${suffix}`;
      }
    );
}

function transpileFile(sourcePath) {
  const relativePath = path.relative(projectRoot, sourcePath);
  const outputPath = path.join(tempRoot, relativePath).replace(/\.(ts|tsx)$/, '.js');
  fs.mkdirSync(path.dirname(outputPath), { recursive: true });

  const source = fs.readFileSync(sourcePath, 'utf8');
  const transpiled = ts.transpileModule(source, {
    compilerOptions: {
      target: ts.ScriptTarget.ES2022,
      module: ts.ModuleKind.ESNext,
      jsx: ts.JsxEmit.ReactJSX,
      esModuleInterop: true,
      allowSyntheticDefaultImports: true,
      moduleResolution: ts.ModuleResolutionKind.Bundler
    },
    fileName: sourcePath
  });

  fs.writeFileSync(outputPath, rewriteRelativeSpecifiers(transpiled.outputText), 'utf8');
}

function writeNextShims() {
  const nextShimDir = path.join(tempRoot, 'node_modules', 'next');
  fs.mkdirSync(nextShimDir, { recursive: true });

  fs.writeFileSync(
    path.join(nextShimDir, 'package.json'),
    JSON.stringify(
      {
        name: 'next',
        type: 'module',
        exports: {
          './image': './image.js',
          './navigation': './navigation.js'
        }
      },
      null,
      2
    ),
    'utf8'
  );

  fs.writeFileSync(
    path.join(nextShimDir, 'image.js'),
    "import { createElement } from 'react';\nexport default function Image(props) { return createElement('img', props); }\n",
    'utf8'
  );

  fs.writeFileSync(
    path.join(nextShimDir, 'navigation.js'),
    'export function useRouter() { return { push() {}, refresh() {}, replace() {}, back() {} }; }\n',
    'utf8'
  );
}

for (const root of sourceRoots) {
  const absoluteRoot = path.join(projectRoot, root);
  for (const file of collectFiles(absoluteRoot)) {
    transpileFile(file);
  }
}

writeNextShims();

const testFiles = collectJavaScriptFiles(path.join(tempRoot, 'tests')).map((file) =>
  path.relative(tempRoot, file)
);

if (testFiles.length === 0) {
  console.error('No tests found.');
  process.exit(1);
}

try {
  const result = spawnSync(process.execPath, ['--test', ...testFiles], {
    cwd: tempRoot,
    stdio: 'inherit'
  });

  process.exit(result.status ?? 1);
} finally {
  fs.rmSync(tempRoot, { recursive: true, force: true });
}
