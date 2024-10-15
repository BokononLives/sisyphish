FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG PACKAGE_SOURCE_PATH
ARG PACKAGE_SOURCE_USERNAME
ARG PACKAGE_SOURCE_PASSWORD

WORKDIR /src
COPY ["sisyphish/sisyphish.csproj", "sisyphish/"]

COPY . .
WORKDIR "/src/sisyphish"
RUN dotnet build "sisyphish.csproj" -c Release -o /app/build


FROM build AS publish
RUN dotnet publish "sisyphish.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "sisyphish.dll"]