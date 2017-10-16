@ECHO OFF
SET BatchPath=%~dp0
cd "%BatchPath%"
powershell -STA -ExecutionPolicy Bypass -NoExit -File Build.ps1 -Configuration Debug -Action Test
pause