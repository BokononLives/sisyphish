FROM alpine AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
RUN apt-get update && apt-get install -y clang musl-tools zlib1g-dev && rm -rf /var/lib/apt/lists/*
WORKDIR /src
COPY ["sisyphish/sisyphish.csproj", "sisyphish/"]
RUN dotnet restore "sisyphish/sisyphish.csproj"
COPY . .

WORKDIR /src/sisyphish
RUN dotnet publish "sisyphish.csproj" -c Release -r linux-musl-x64 --self-contained true -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["./sisyphish"]
