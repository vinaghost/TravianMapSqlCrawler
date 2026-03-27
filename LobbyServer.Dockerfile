FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /source

COPY ServerScanner/ServerScanner.csproj ServerScanner/
COPY ServerScanner/packages.lock.json ServerScanner/

RUN dotnet restore ServerScanner/ServerScanner.csproj /p:RestorePackagesWithLockFile=true /p:PublishReadyToRun=true -a $TARGETARCH 

COPY ServerScanner/ ServerScanner/
RUN dotnet publish ServerScanner/ServerScanner.csproj /p:PublishReadyToRun=true -a $TARGETARCH --self-contained --no-restore -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS final
WORKDIR /app
COPY --from=build /app .
RUN chmod +x ServerScanner
ENTRYPOINT ["./ServerScanner"]