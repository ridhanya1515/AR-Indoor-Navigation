@echo off
cd /d "%~dp0"
set /p campus_username=Enter a username: 
py -3 server.py --add-user "%campus_username%"
pause
