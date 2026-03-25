FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY MathComLib/MathComLib.csproj ./MathComLib/
COPY ApiGateway/ApiGateway.csproj ./ApiGateway/
RUN dotnet restore ./ApiGateway/ApiGateway.csproj
COPY MathComLib/ ./MathComLib/
COPY ApiGateway/ ./ApiGateway/
RUN dotnet publish ./ApiGateway/ApiGateway.csproj -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out ./
EXPOSE 80
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
