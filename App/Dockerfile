FROM mcr.microsoft.com/dotnet/sdk:8.0 AS prepare-restore-files
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet tool install --global --no-cache dotnet-subset --version 0.3.2
WORKDIR /source
COPY . .
RUN dotnet subset restore App/App.csproj --root-directory /source --output restore_subset/

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
WORKDIR /src
COPY --from=prepare-restore-files /source/restore_subset .
RUN dotnet restore App/App.csproj --locked-mode

# copy source code, add more if required other project
COPY App/ App/
RUN dotnet publish App/App.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=publish /app/publish .
RUN ["chmod", "+x", "App"]
ENTRYPOINT ["./App"]