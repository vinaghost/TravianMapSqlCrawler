FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /source

COPY ServerScanner/ServerScanner.csproj ServerScanner/
COPY ServerScanner/packages.lock.json ServerScanner/

RUN dotnet restore ServerScanner/ServerScanner.csproj /p:RestorePackagesWithLockFile=true /p:PublishReadyToRun=true -a $TARGETARCH 

COPY ServerScanner/ ServerScanner/
RUN dotnet publish ServerScanner/ServerScanner.csproj /p:PublishReadyToRun=true -a $TARGETARCH --self-contained --no-restore -c Release -o /app
WORKDIR /app
RUN pwsh playwright.ps1 install firefox

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS final
RUN apt-get update && apt-get install -y --no-install-recommends \
    libxcb-shm0 \
    libx11-xcb1 \
    libx11-6 \
    libxcb1 \
    libxext6 \
    libxrandr2 \
    libxcomposite1 \
    libxcursor1 \
    libxdamage1 \
    libxfixes3 \
    libxi6 \
    libgtk-3-0 \
    libpangocairo-1.0-0 \
    libpango-1.0-0 \
    libatk1.0-0 \
    libcairo-gobject2 \
    libcairo2 \
    libgdk-pixbuf-2.0-0 \
    libglib2.0-0 \
    libxrender1 \
    libasound2t64 \
    libfreetype6 \
    libfontconfig1 \
    libdbus-1-3 && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*
	
COPY --from=build /root/.cache/ms-playwright/ /root/.cache/ms-playwright/
WORKDIR /app
COPY --from=build /app .
RUN chmod +x ServerScanner
ENTRYPOINT ["./ServerScanner"]