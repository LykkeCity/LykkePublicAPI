FROM microsoft/dotnet:2.1.0-aspnetcore-runtime
ENTRYPOINT ["dotnet", "LykkePublicAPI.dll"]
ARG source=.
WORKDIR /app
COPY $source .