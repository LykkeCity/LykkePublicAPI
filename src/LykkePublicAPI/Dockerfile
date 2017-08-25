FROM microsoft/aspnetcore:2.0
ENTRYPOINT ["dotnet", "LykkePublicAPI.dll"]
ARG source=.
WORKDIR /app
COPY $source .
COPY /runtimes/linux-x64/native/libuv.so /app
#RUN ln -s /runtimes/debian-x64/native/libuv.so /app/libuv.so