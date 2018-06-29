FROM microsoft/dotnet:2.1-aspnetcore-runtime
ENTRYPOINT ["dotnet", "LykkePublicAPI.dll"]
ARG source=.
WORKDIR /app
COPY $source .