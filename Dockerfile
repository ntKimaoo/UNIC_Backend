FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Presentation/UNIC.Presentation.csproj", "Presentation/"]
COPY ["BusinessLogic/UNIC.BusinessLogic.csproj", "BusinessLogic/"]
COPY ["DataAccess/UNIC.DataAccess.csproj", "DataAccess/"]

RUN dotnet restore "Presentation/UNIC.Presentation.csproj"

COPY . .

WORKDIR "/src/Presentation"
RUN dotnet publish "UNIC.Presentation.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "UNIC.Presentation.dll"]