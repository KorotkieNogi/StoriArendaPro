#FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
#WORKDIR /app
#EXPOSE 80
#EXPOSE 443
#
#FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
#WORKDIR /src
#COPY ["StoriArendaPro.csproj", "."]
#RUN dotnet restore "StoriArendaPro.csproj"
#COPY . .
#RUN dotnet build "StoriArendaPro.csproj" -c Release -o /app/build
#
#FROM build AS publish
#RUN dotnet publish "StoriArendaPro.csproj" -c Release -o /app/publish
#
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
## Простой запуск - логика ожидания в коде приложения
#ENTRYPOINT ["dotnet", "StoriArendaPro.dll"]