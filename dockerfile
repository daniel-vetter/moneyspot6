FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build_backend
WORKDIR /source
COPY src/backend .
RUN dotnet publish -c release -o /app

FROM node:22 AS build_frontend
WORKDIR /source
COPY src/frontend .
RUN npm install && npm run build

FROM amazoncorretto:21-alpine3.22-jdk AS build_hbci-adapter
WORKDIR /source
COPY src/hbci-adapter .
RUN chmod +x ./gradlew && ./gradlew --no-daemon jar

FROM alpine:3.21.3
WORKDIR /app
RUN apk update
RUN apk add openjdk21 aspnetcore9-runtime
COPY --from=build_backend /app /app
COPY --from=build_frontend /source/dist/money-spot6.client/browser /app/wwwroot
COPY --from=build_hbci-adapter /source/build/libs/HbciAdapter6-1.0-SNAPSHOT.jar /app/hbci-adapter/HbciAdapter6.jar
ARG BUILD_TIME
ARG BUILD_COMMIT
ENV BUILD_TIME=$BUILD_TIME
ENV BUILD_COMMIT=$BUILD_COMMIT
ENV ASPNETCORE_URLS="http://0.0.0.0:80"
ENTRYPOINT [ "dotnet", "MoneySpot6.WebApp.dll" ]