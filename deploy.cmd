@echo off
docker build -t moneyspot6 .
if not exist "build" mkdir build
docker save moneyspot6 > build\moneyspot6.img
pause