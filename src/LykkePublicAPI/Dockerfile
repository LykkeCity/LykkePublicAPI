FROM microsoft/aspnetcore:1.1.1
ENTRYPOINT ["dotnet", "LykkePublicAPI.dll"]
ARG source=.
WORKDIR /app
COPY $source .