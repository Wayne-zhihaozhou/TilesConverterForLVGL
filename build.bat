@echo off
REM Build the C++ LVGL tile converter with MSVC (Visual Studio 2022 BuildTools).
REM Requires the "C++ desktop development" workload so cl.exe is present.
REM   build.bat          -> bin\Release\lvglConverter.exe
REM   build.bat clean    -> remove build artifacts

setlocal
set ROOT=%~dp0
set SRC=%ROOT%src
set BUILD=%ROOT%build
set OUT=%ROOT%bin\Release

if "%1"=="clean" (
    rmdir /s /q "%BUILD%" 2>nul
    if exist "%OUT%\lvglConverter.exe" del "%OUT%\lvglConverter.exe"
    echo Cleaned.
    endlocal
    exit /b 0
)

if not exist "%BUILD%" mkdir "%BUILD%"
if not exist "%OUT%" mkdir "%OUT%"

REM Configure the x64 toolchain (sets INCLUDE/LIB/PATH for this shell).
call "%ProgramFiles(x86)%\Microsoft Visual Studio\2022\BuildTools\Common7\Tools\VsDevCmd.bat" -arch=x64 >nul 2>&1
if errorlevel 1 (
    echo Error: VsDevCmd.bat not found. Install the C++ desktop development workload.
    endlocal
    exit /b 1
)

REM Compile from inside build/ so intermediate .obj files land there (no /Fo needed).
cd /d "%BUILD%"
cl.exe /O2 /Oy /std:c++17 /EHsc /W3 /utf-8 ^
    /I"%SRC%" ^
    "%SRC%\png_decoder.c" ^
    "%SRC%\lvgl_converter.cpp" ^
    /Fe:"%OUT%\lvglConverter.exe"

if errorlevel 1 (
    echo Build failed.
    endlocal
    exit /b 1
)
echo Built: %OUT%\lvglConverter.exe
endlocal