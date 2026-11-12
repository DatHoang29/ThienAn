#!/usr/bin/env node

// Khởi chạy DAB MCP (stdio) cho một môi trường DB; chuỗi kết nối lấy từ .agents/mcp/.env hoặc biến môi trường.
// stdout là kênh MCP: thông báo lỗi chỉ ghi ra stderr và không bao giờ in chuỗi kết nối.

import {spawn} from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import process from 'node:process';

const ENVIRONMENTS = ['dev', 'test', 'staging', 'inbound'];
const root = path.resolve(import.meta.dirname, '../..');

function fail(message) {
  console.error(`mcp/dab: ${message}`);
  process.exit(1);
}

function parseEnvFile(text) {
  const values = {};
  for (const rawLine of text.split(/\r?\n/)) {
    const line = rawLine.trim();
    if (!line || line.startsWith('#')) continue;
    const separator = line.indexOf('=');
    if (separator <= 0) continue;
    // Chuỗi kết nối chứa cả '=' và ';' nên chỉ tách ở dấu '=' đầu tiên.
    let value = line.slice(separator + 1).trim();
    if (value.length >= 2 && (value[0] === '"' || value[0] === "'") && value.at(-1) === value[0]) value = value.slice(1, -1);
    values[line.slice(0, separator).trim()] = value;
  }
  return values;
}

function parseArgs(argv) {
  const [environment, ...rest] = argv;
  if (!ENVIRONMENTS.includes(environment)) fail(`môi trường phải là một trong ${ENVIRONMENTS.join('|')}.`);
  let envFile = path.join(import.meta.dirname, '.env');
  for (let i = 0; i < rest.length; i += 1) {
    if (rest[i] === '--env-file') envFile = path.resolve(rest[++i] ?? '');
    else fail(`tham số không hợp lệ: ${rest[i]}`);
  }
  return {environment, envFile};
}

const {environment, envFile} = parseArgs(process.argv.slice(2));
const key = `DAB_${environment.toUpperCase()}_CONNECTION_STRING`;
const fileValues = fs.existsSync(envFile) ? parseEnvFile(fs.readFileSync(envFile, 'utf8')) : {};
const connectionString = process.env[key] || fileValues[key];
if (!connectionString) {
  fail(`thiếu ${key}. Đặt trong ${envFile} (sao chép từ .agents/mcp/.env.example) hoặc trong biến môi trường.`);
}

// cwd = gốc repo để `dotnet tool run` dùng đúng phiên bản DAB ghim trong dotnet-tools.json.
const child = spawn('dotnet', [
  'tool', 'run', '--allow-roll-forward', 'dab', 'start', '--mcp-stdio', 'role:anonymous',
  '--LogLevel', 'Error', '--config', path.join(import.meta.dirname, 'dab-config.json')
], {
  cwd: root,
  stdio: 'inherit',
  env: {...process.env, ASPNETCORE_URLS: 'http://127.0.0.1:0', DAB_CONNECTION_STRING: connectionString}
});

for (const signal of ['SIGINT', 'SIGTERM']) process.on(signal, () => child.kill(signal));
child.on('error', error => fail(`không chạy được dotnet: ${error.message}`));
child.on('exit', (code, signal) => process.exit(code ?? (signal ? 1 : 0)));
