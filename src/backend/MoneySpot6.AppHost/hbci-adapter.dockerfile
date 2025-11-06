FROM amazoncorretto:21-alpine3.22-jdk AS build_hbci-adapter
WORKDIR /source
COPY . .
RUN chmod +x ./gradlew && ./gradlew --no-daemon jar

FROM alpine:3.21.3
WORKDIR /app
RUN apk add openjdk21
COPY --from=build_hbci-adapter /source/build/libs/HbciAdapter6-1.0-SNAPSHOT.jar /app/hbci-adapter/HbciAdapter6.jar
ENTRYPOINT [ "sleep", "infinity" ]