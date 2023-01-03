#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM mcr.microsoft.com/dotnet/aspnet:7.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0-buster-slim AS build
WORKDIR /src
COPY KeyValueStore.csproj .
RUN dotnet restore "./KeyValueStore.csproj"
COPY . .
RUN dotnet build "KeyValueStore.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KeyValueStore.csproj" -c Release -o /app/publish
RUN apt-get install curl
RUN curl --create-dirs -o /app/root.crt -O https://cockroachlabs.cloud/clusters/85ff5b6e-a9ef-4baa-a2b9-aba8bb037a0f/cert

FROM base AS final

ARG GIT_COMMIT
ENV GIT_COMMIT=$GIT_COMMIT

WORKDIR /app
RUN apt update &&  apt install -y libc-dev
RUN mkdir -p /root/.postgresql
COPY --from=publish /app/root.crt /root/.postgresql/
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KeyValueStore.dll"]