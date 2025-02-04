FROM alpine AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG PACKAGE_SOURCE_PATH
ARG PACKAGE_SOURCE_USERNAME
ARG PACKAGE_SOURCE_PASSWORD

RUN apk update && apk upgrade
RUN apk add --no-cache clang build-base zlib-dev

WORKDIR /src
COPY ["sisyphish/sisyphish.csproj", "sisyphish/"]
RUN dotnet restore "sisyphish/sisyphish.csproj"

COPY . .
WORKDIR "/src/sisyphish"
RUN dotnet publish "sisyphish.csproj" -c Release -r linux-musl-x64 -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["./sisyphish"]