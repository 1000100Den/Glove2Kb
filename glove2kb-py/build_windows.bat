@echo off
rem Glove2Kb-Py - Windows build script
rem Requires: Python 3.10+ installed and added to PATH.
rem Run this file in the project folder that contains run.py and src\.
setlocal
cd /d %~dp0

if not exist run.py (
    echo ERROR: run.py was not found in this folder:
    echo   %CD%
    echo.
    echo This script must be placed in the "glove2kb-py" project folder
    echo together with run.py, src\, Glove2Kb-Py.spec and the other
    echo project files. If you downloaded a ZIP of the repository,
    echo use the inner "glove2kb-py" folder, not the repository root.
    echo.
    pause
    exit /b 1
)
if not exist src\glove2kb_py\app.py (
    echo ERROR: src\glove2kb_py\app.py was not found.
    echo The whole project folder is needed, not only the build files.
    echo.
    pause
    exit /b 1
)

python -m venv .venv-build
if errorlevel 1 goto :error
call .venv-build\Scripts\activate.bat

python -m pip install --upgrade pip
python -m pip install pyinstaller
if errorlevel 1 goto :error

pyinstaller --noconfirm Glove2Kb-Py.spec
if errorlevel 1 goto :error

echo.
echo Build finished: dist\Glove2Kb-Py.exe
pause
exit /b 0

:error
echo.
echo Build failed. See the messages above.
pause
exit /b 1
