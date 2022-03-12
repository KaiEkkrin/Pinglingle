# From a suggestion I found at
# https://chrissainty.com/containerising-blazor-applications-with-docker-containerising-a-blazor-server-app/
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
COPY Client/ /Client
COPY Server/ /Server
COPY Shared/ /Shared
WORKDIR /Server
RUN dotnet restore "Pinglingle.Server.csproj"
RUN dotnet build "Pinglingle.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Pinglingle.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Pinglingle.Server.dll"]