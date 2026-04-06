FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Backend.csproj ./
RUN dotnet restore Backend.csproj

COPY . .
RUN dotnet publish Backend.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./
COPY Data ./Data

ENTRYPOINT ["dotnet", "Backend.dll"]
