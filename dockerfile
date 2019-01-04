FROM microsoft/dotnet:aspnetcore-runtime
WORKDIR /dotnetapp
COPY ./build .
ENTRYPOINT ["dotnet", "NosCoreBot.dll"]
