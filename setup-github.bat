@echo off
REM ═══════════════════════════════════════════════════════════════════
REM  GridAcademy — GitHub setup script
REM  Run this ONCE after creating your GitHub repo.
REM
REM  BEFORE RUNNING:
REM  1. Create a new repo at https://github.com/new  (name: gridacademy-api)
REM     ▸ Private repo recommended
REM     ▸ Do NOT add README / .gitignore (we have our own)
REM  2. Replace YOUR_GITHUB_USERNAME below with your actual username
REM ═══════════════════════════════════════════════════════════════════

set GITHUB_USER=gridtech-labs
set REPO_NAME=GridAcademy

cd /d D:\Bkp\GRID\GridAcademy

echo.
echo [1/4] Initialising git repository...
git init
git branch -M main

echo.
echo [2/4] Staging all files...
git add .

echo.
echo [3/4] Creating initial commit...
git commit -m "Initial commit: GridAcademy API + Admin Panel"

echo.
echo [4/4] Pushing to GitHub...
git remote add origin https://github.com/%GITHUB_USER%/%REPO_NAME%.git
git push -u origin main

echo.
echo ✅ Done! Your code is now on GitHub.
echo    Repository: https://github.com/%GITHUB_USER%/%REPO_NAME%
echo.
echo Next step: Go to https://railway.app and deploy from this GitHub repo.
echo See the RAILWAY-SETUP.md file in this folder for instructions.
pause
