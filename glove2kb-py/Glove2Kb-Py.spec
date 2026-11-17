# PyInstaller spec for Glove2Kb-Py (one-file, windowed).
# Build:  pyinstaller Glove2Kb-Py.spec
# Works on Windows and Linux; the output binary is for the OS you build on.
import os

PROJECT_ROOT = os.path.abspath(SPECPATH)

a = Analysis(
    [os.path.join(PROJECT_ROOT, "run.py")],
    pathex=[os.path.join(PROJECT_ROOT, "src")],
    binaries=[],
    datas=[],
    hiddenimports=[],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[],
    noarchive=False,
)
pyz = PYZ(a.pure)

exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.datas,
    [],
    name="Glove2Kb-Py",
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=False,
    console=False,
    disable_windowed_traceback=False,
)
