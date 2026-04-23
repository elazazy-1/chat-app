@echo off
REM Navigate to project directory
cd /d C:\Users\user\Downloads\MauiApp3

REM 1. Initialize git repository
echo === 1. Initializing git repository ===
git init

REM 2. Configure git user
echo.
echo === 2. Configuring git user ===
git config user.name "elazazy-1"

REM 3. Configure git email
echo.
echo === 3. Configuring git email ===
git config user.email "elazazy-1@github.com"

REM 4. Stage all files
echo.
echo === 4. Staging all files ===
git add .

REM 5. Check status
echo.
echo === 5. Git status before commit ===
git status

REM 6. Create initial commit
echo.
echo === 6. Creating initial commit ===
git commit -m "Initial commit: Create MAUI cross-platform application"^
 "- Add project structure for Android, iOS, macOS, and Windows platforms"^
 "- Configure MAUI dependencies (Microsoft.Maui.Controls, Plugin.Maui.Audio)"^
 "- Add comprehensive README with setup and build instructions"^
 "- Add .gitignore for .NET MAUI projects"

REM 7. Add remote origin
echo.
echo === 7. Adding remote origin ===
git remote add origin https://github.com/elazazy-1/DS-Project.git

REM 8. Show git status after all operations
echo.
echo === 8. Final git status ===
git status

REM Show remote info
echo.
echo === Remote Configuration ===
git remote -v

REM Show commit log
echo.
echo === Commit Log ===
git log --oneline

pause
