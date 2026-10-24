const { app, BrowserWindow } = require('electron');
const http = require('http');
const fs = require('fs');
const path = require('path');

const probe = process.argv.includes('--probe');
const packaged = process.argv.includes('--packaged');
const devUrl = process.env.AANGAN_URL || 'http://localhost:4200';
const distDir = path.join(__dirname, '..', 'dist', 'client', 'browser');

function contentType(file) {
  if (file.endsWith('.js')) return 'text/javascript; charset=utf-8';
  if (file.endsWith('.css')) return 'text/css; charset=utf-8';
  if (file.endsWith('.html')) return 'text/html; charset=utf-8';
  if (file.endsWith('.svg')) return 'image/svg+xml';
  if (file.endsWith('.json')) return 'application/json';
  if (file.endsWith('.woff2')) return 'font/woff2';
  return 'application/octet-stream';
}

function serveDist() {
  return new Promise((resolve, reject) => {
    const server = http.createServer((request, response) => {
      const url = new URL(request.url || '/', 'http://127.0.0.1');
      const relative = decodeURIComponent(url.pathname).replace(/^\/+/, '');
      const root = path.resolve(distDir);
      const requested = path.resolve(root, relative);
      const inside = requested.startsWith(root + path.sep) && fs.existsSync(requested) && fs.statSync(requested).isFile();
      const file = inside ? requested : path.join(root, 'index.html');
      response.writeHead(200, { 'Content-Type': contentType(file) });
      fs.createReadStream(file).pipe(response);
    });
    server.listen(4210, '127.0.0.1', () => resolve(server));
    server.on('error', reject);
  });
}

async function createWindow() {
  const win = new BrowserWindow({
    width: 1280,
    height: 800,
    minWidth: 960,
    minHeight: 640,
    show: !probe,
    title: 'Aangan',
    backgroundColor: '#1b1410',
    autoHideMenuBar: true,
    webPreferences: {
      preload: path.join(__dirname, 'preload.cjs'),
      contextIsolation: true,
      nodeIntegration: false,
    },
  });

  const url = packaged ? 'http://127.0.0.1:4210/' : devUrl;
  if (packaged) await serveDist();

  if (probe) {
    win.webContents.on('did-fail-load', (_event, code, description) => {
      console.error(JSON.stringify({ ok: false, error: description, code }));
      app.exit(1);
    });
  }

  await win.loadURL(url);

  if (!probe) return;

  try {
    const result = await win.webContents.executeJavaScript(`(async () => {
      const started = Date.now();
      while (!document.querySelector('h1') && Date.now() - started < 8000) {
        await new Promise((resolve) => setTimeout(resolve, 100));
      }
      const health = await fetch('http://localhost:5254/api/health').then((response) => response.json());
      return {
        title: document.title,
        shell: window.aanganShell || null,
        heading: document.querySelector('h1')?.innerText || '',
        server: !!document.querySelector('input[placeholder="http://192.168.1.20:5254"]'),
        health
      };
    })()`);
    console.log(JSON.stringify({ ok: true, ...result }));
    app.exit(result.shell === 'desktop' && result.health?.product === 'Aangan' ? 0 : 1);
  } catch (error) {
    console.error(JSON.stringify({ ok: false, error: String(error) }));
    app.exit(1);
  }
}

app.whenReady().then(createWindow);
app.on('window-all-closed', () => app.quit());
