FROM alpine AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/nightly/sdk:10.0-alpine AS build
RUN apk update && apk upgrade && apk add --no-cache clang build-base binutils musl-dev zlib-dev
WORKDIR /src
COPY ["sisyphish/sisyphish.csproj", "sisyphish/"]
RUN dotnet restore "sisyphish/sisyphish.csproj"
COPY . .

WORKDIR /src/sisyphish
RUN dotnet publish "sisyphish.csproj" -c Release -r linux-musl-x64 -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["./sisyphish"]
