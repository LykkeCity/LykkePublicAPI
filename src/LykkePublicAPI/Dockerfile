FROM microsoft/aspnetcore:2.0
ENTRYPOINT ["dotnet", "LykkePublicAPI.dll"]
ARG source=.
WORKDIR /app
COPY $source .