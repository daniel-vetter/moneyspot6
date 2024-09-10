FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build_backend
WORKDIR /source
COPY src/backend .
RUN dotnet publish -c release -o /app

FROM node:22 AS build_frontend
WORKDIR /source
COPY src/frontend .
RUN npm install && npm run build

FROM openjdk:21 AS build_hbci-adapter
WORKDIR /source
COPY src/hbci-adapter .
RUN ./gradlew --no-daemon jar

FROM alpine
WORKDIR /app
RUN apk update
RUN apk add openjdk21 aspnetcore8-runtime
COPY --from=build_backend /app /app
COPY --from=build_frontend /source/dist/money-spot6.client/browser /app/wwwroot
COPY --from=build_hbci-adapter /source/build/libs/HbciAdapter6-1.0-SNAPSHOT.jar /app/hbci-adapter/HbciAdapter6.jar
ENV ASPNETCORE_URLS="http://0.0.0.0:80"
ENTRYPOINT [ "dotnet", "MoneySpot6.WebApp.dll" ]