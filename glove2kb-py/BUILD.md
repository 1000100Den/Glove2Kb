# Building a standalone executable / 実行ファイルのビルド

Glove2Kb-Py can be packaged as a single standalone executable with
PyInstaller. The executable is **OS-specific**: build on the OS you want
to run it on. A Windows `.exe` must be built on Windows; a Linux binary
must be built on Linux.

実行ファイルはOSごとにビルドが必要です。Windows版（.exe）はWindows上で、
Linux版はLinux上でビルドしてください。

## Windows

Requirements: Python 3.10 or later (check "Add python.exe to PATH" during
installation).

Run the script in the **project folder** — the `glove2kb-py` folder that
contains `run.py` and `src\`. If you downloaded a ZIP of the repository,
that is the inner `glove2kb-py` folder, not the repository root. Copying
only the build files (`build_windows.bat`, `Glove2Kb-Py.spec`) into an
empty folder does not work: PyInstaller fails with
`script '...\run.py' not found`.

1. Double-click `build_windows.bat`, or run it from a command prompt.
   It creates a virtual environment, installs PyInstaller, and builds.
2. The result is `dist\Glove2Kb-Py.exe` (single file, no console window).

Manual equivalent:

```bat
python -m venv .venv-build
.venv-build\Scripts\activate.bat
pip install pyinstaller
pyinstaller --noconfirm Glove2Kb-Py.spec
```

## Linux

```bash
python3 -m venv .venv-build
.venv-build/bin/pip install pyinstaller
.venv-build/bin/pyinstaller --noconfirm Glove2Kb-Py.spec
# result: dist/Glove2Kb-Py
```

## Notes

- The app uses only the Python standard library (Tkinter for the GUI),
  so PyInstaller needs no extra options or hidden imports.
- `Glove2Kb-Py.spec` is the shared build definition (one-file, windowed).
- Unsigned executables may trigger an OS warning on first launch
  (Windows SmartScreen / macOS Gatekeeper style prompts). This is normal
  for self-built binaries.
