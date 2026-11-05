# Frontend (React Router 8, SPA mode)

Static single-page app (`ssr: false`, DEC-GEN-027). Design tokens live in `app/app.css` and follow
`docs/DESIGN.md`.

```powershell
npm install
npm run dev          # http://localhost:5173, /api is proxied to the API on :5080
npm run typecheck
npm run lint
npm run test
npm run build        # static files in build/client
```
