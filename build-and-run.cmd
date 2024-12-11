@echo off
docker build -t moneyspot .
docker run -it -p 8000:80 -e ASPNETCORE_ENVIRONMENT="Development" moneyspot
pause