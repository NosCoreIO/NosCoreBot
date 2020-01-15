FROM microsoft/dotnet:aspnetcore-runtime
WORKDIR /dotnetapp
COPY /home/travis/build/NosCoreIO/NosCoreBot/bin/Docker/ .
ENTRYPOINT ["dotnet", "NosCoreBot.dll"]
