@ECHO OFF
SET BatchPath=%~dp0
cd "%BatchPath%"
powershell -STA -ExecutionPolicy Bypass -File Build.ps1 -Configuration Release -Action Deploy
pause