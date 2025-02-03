FROM debian:bookworm-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG PACKAGE_SOURCE_PATH
ARG PACKAGE_SOURCE_USERNAME
ARG PACKAGE_SOURCE_PASSWORD

RUN apt-get update && apt-get install -y clang libc6-dev

WORKDIR /src
COPY ["sisyphish/sisyphish.csproj", "sisyphish/"]
RUN dotnet restore "sisyphish/sisyphish.csproj"

COPY . .
WORKDIR "/src/sisyphish"
RUN dotnet publish "sisyphish.csproj" -c Release -r linux-x64 -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "sisyphish.dll"]