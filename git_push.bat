@echo off
REM This batch file pushes the repository to GitHub

cd /d "C:\Users\user\Downloads\MauiApp3"

echo ========================================
echo Pushing to GitHub - DS Project
echo ========================================
echo.

echo [1/3] Renaming branch to main...
git branch -M main
echo.

echo [2/3] Pushing to GitHub...
echo Enter your GitHub credentials when prompted
echo (Use your personal access token as password if 2FA is enabled)
echo.
git push -u origin main
echo.

echo [3/3] Verifying remote...
git remote -v
echo.

echo ========================================
echo Push Complete!
echo ========================================
echo.
echo Your repository is now live at:
echo https://github.com/elazazy-1/DS-Project
echo.
pause
