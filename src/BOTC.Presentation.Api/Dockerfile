FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore src/BOTC.Presentation.Api/BOTC.Presentation.Api.csproj
RUN dotnet publish src/BOTC.Presentation.Api/BOTC.Presentation.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000

EXPOSE 10000

ENTRYPOINT ["dotnet", "BOTC.Presentation.Api.dll"]