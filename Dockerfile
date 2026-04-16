FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["croupe 06 TournoiGolf.csproj", "."]
RUN dotnet restore "croupe 06 TournoiGolf.csproj"
COPY . .
RUN dotnet publish "croupe 06 TournoiGolf.csproj" -c Release -o /app/publish -p:TreatWarningsAsErrors=false -warnaserror-

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "croupe 06 TournoiGolf.dll"]
