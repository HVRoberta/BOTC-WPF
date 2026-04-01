FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY BOTC.sln ./
COPY src/BOTC.Application/BOTC.Application.csproj src/BOTC.Application/
COPY src/BOTC.Contracts/BOTC.Contracts.csproj src/BOTC.Contracts/
COPY src/BOTC.Infrastructure/BOTC.Infrastructure.csproj src/BOTC.Infrastructure/
COPY src/BOTC.Presentation.Api/BOTC.Presentation.Api.csproj src/BOTC.Presentation.Api/

RUN dotnet restore src/BOTC.Presentation.Api/BOTC.Presentation.Api.csproj

COPY . .
RUN dotnet publish src/BOTC.Presentation.Api/BOTC.Presentation.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "BOTC.Presentation.Api.dll"]
