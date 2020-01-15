FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine

WORKDIR /app
COPY ./bin/Docker .
ENTRYPOINT ["dotnet", "NosCoreBot.dll"]
