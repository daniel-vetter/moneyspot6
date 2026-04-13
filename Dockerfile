FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS backend
WORKDIR /src
COPY src/backend/ .
RUN dotnet publish MoneySpot6.WebApp/MoneySpot6.WebApp.csproj -c Release -o /publish

FROM node:22-alpine AS frontend
WORKDIR /src
COPY src/frontend/package.json src/frontend/package-lock.json ./
RUN npm ci
COPY src/frontend/ .
RUN npm run build

FROM amazoncorretto:21-alpine AS hbci
WORKDIR /src
COPY src/hbci-adapter/ .
RUN chmod +x ./gradlew && ./gradlew jar

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine3.22
WORKDIR /app
RUN apk update && apk add openjdk21
COPY --from=backend /publish /app
COPY --from=frontend /src/dist/money-spot6.client/browser /app/wwwroot
COPY --from=hbci /src/build/libs/HbciAdapter6-1.0-SNAPSHOT.jar /app/hbci-adapter/HbciAdapter6.jar
ARG BUILD_VERSION=unknown
LABEL build.version=$BUILD_VERSION
ENV ASPNETCORE_URLS="http://0.0.0.0:80"
ENTRYPOINT [ "dotnet", "MoneySpot6.WebApp.dll" ]
