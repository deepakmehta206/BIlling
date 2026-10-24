# Phase 6 — desktop and phone

The same Angular app, in two more windows. Neither shell has its own screens. Both call the API.

## Desktop

`client/electron` is the counter window. From `client`, with the web dev server running:

```powershell
npm run desktop
```

The window loads `http://localhost:4200`. The preload marks the page as the desktop shell, so sign-in shows a Server field. Leave it at `http://localhost:5254` when the API is on this computer.

`npm run desktop:pack` serves `dist/client/browser` on `http://127.0.0.1:4210` and loads that. Run `npm run build` first. The renderer has no Node access.

## Phone

`client/capacitor.config.ts` points Capacitor at the Angular build. The Android project lives in `client/android`. App id `local.aangan.desk`.

```powershell
npm run mobile:sync
```

That builds the web app and copies it into Android. The WebView uses cleartext HTTP so it can reach the API on the LAN. On the phone, set Server to the computer's address. The saved address is `aangan.apiOrigin` and is used for both the API and the live floor connection.

iOS uses the same config. Adding it needs a Mac: `npx cap add ios`.

## API

The browser origins stay `localhost:4200` and `127.0.0.1:4200`. The shells also send `http://127.0.0.1:4210`, `http://localhost`, `https://localhost`, and `capacitor://localhost`.
