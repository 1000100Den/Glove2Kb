@echo off
rem Glove2Kb-Py - Windows build script
rem Requires: Python 3.10+ installed and added to PATH.
setlocal
cd /d %~dp0

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
