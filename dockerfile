FROM microsoft/dotnet:aspnetcore-runtime
WORKDIR /dotnetapp
COPY ./build/netcoreapp2.2 .
ENTRYPOINT ["dotnet", "NosCoreBot.dll"]
