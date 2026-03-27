# Railway Deployment Guide — GridAcademy API + Admin Panel

## Step 1 — Push code to GitHub

1. Go to https://github.com/new
2. Create a **private** repository named `gridacademy-api`
3. Do NOT add README / .gitignore (we have our own)
4. Open `setup-github.bat`, replace `YOUR_GITHUB_USERNAME` with your GitHub username
5. Run `setup-github.bat` — it initialises git, commits, and pushes

---

## Step 2 — Create Railway project

1. Go to https://railway.app → **New Project**
2. Click **"Deploy from GitHub repo"**
3. Connect your GitHub account if not already done
4. Select the `gridacademy-api` repository
5. Railway will detect the `Dockerfile` automatically → click **Deploy**

---

## Step 3 — Add PostgreSQL database

1. In your Railway project dashboard, click **"+ New"** → **"Database"** → **"PostgreSQL"**
2. Railway creates the database and automatically sets `DATABASE_URL` in your service's environment
3. No manual connection string needed — the app reads `DATABASE_URL` automatically

---

## Step 4 — Set environment variables

In Railway → your service → **Variables** tab, add:

| Variable | Value | Notes |
|----------|-------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Required |
| `Jwt__Secret` | *(generate below)* | Min 32 chars, random |
| `Jwt__Issuer` | `GridAcademy` | |
| `Jwt__Audience` | `GridAcademy` | |
| `AllowedOrigins__0` | `https://your-app.vercel.app` | Your Vercel frontend URL |
| `AllowedOrigins__1` | `https://gridacademy.com` | Your custom domain (if any) |

**Generate a secure JWT secret** (run in any terminal):
```
node -e "console.log(require('crypto').randomBytes(48).toString('base64'))"
```
Or use: https://generate-secret.vercel.app/64

---

## Step 5 — Add a custom domain (optional)

1. Railway → your service → **Settings** → **Domains**
2. Click **"Generate Domain"** for a free `*.railway.app` URL
3. Or add your own domain (e.g. `api.gridacademy.com`) via CNAME

---

## Step 6 — Verify deployment

After Railway finishes building (~2-3 minutes):

- **API health**: `https://your-service.railway.app/swagger`
- **Admin panel**: `https://your-service.railway.app/Admin`
- **Default login**: `admin@gridacademy.com` / `Admin@123!`
- **Change admin password immediately** after first login!

---

## Environment Variables Reference (double-underscore = nested JSON)

```
DATABASE_URL          → auto-set by Railway PostgreSQL plugin
ASPNETCORE_ENVIRONMENT → Production
Jwt__Secret           → your-long-random-secret
Jwt__Issuer           → GridAcademy
Jwt__Audience         → GridAcademy
Jwt__ExpiryMinutes    → 60
AllowedOrigins__0     → https://your-vercel-app.vercel.app
```

---

## Updating the app

After any code change:
```
git add .
git commit -m "describe your change"
git push
```
Railway auto-deploys on every push to `main`.

---

## Troubleshooting

| Problem | Fix |
|---------|-----|
| Build fails | Check Railway logs → Build tab |
| DB connection error | Check `DATABASE_URL` is set in Variables |
| JWT errors | Check `Jwt__Secret` is at least 32 characters |
| CORS errors from frontend | Add Vercel URL to `AllowedOrigins__0` |
| 500 on admin panel | Check `ASPNETCORE_ENVIRONMENT=Production` |
