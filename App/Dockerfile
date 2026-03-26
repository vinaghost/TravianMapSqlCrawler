FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /source

COPY App/App.csproj App/
COPY App/packages.lock.json App/

RUN dotnet restore App/App.csproj /p:RestorePackagesWithLockFile=true /p:PublishReadyToRun=true -a $TARGETARCH 

COPY App/ App/
RUN dotnet publish App/App.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime-deps:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN chmod +x App
ENTRYPOINT ["./App"]