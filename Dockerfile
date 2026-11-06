# ---------- Stage 1: build ----------
# The SDK image contains everything needed to compile .NET apps.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project file first, then download the packages.
# Docker caches this step, so packages are only downloaded again
# when the .csproj changes, not every time the code changes.
COPY src/BankApi/BankApi.csproj src/BankApi/
RUN dotnet restore src/BankApi/BankApi.csproj

# Copy the rest of the source code and publish an optimized Release build.
COPY src/BankApi/ src/BankApi/
RUN dotnet publish src/BankApi/BankApi.csproj -c Release -o /app/publish --no-restore

# ---------- Stage 2: run ----------
# The smaller ASP.NET runtime image: it can run the app, but has no compiler or SDK.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# A folder for the SQLite database, owned by the non-root "app" user so it can write there.
RUN mkdir -p /app/data && chown $APP_UID /app/data

# Take only the finished app from the build stage.
COPY --from=build /app/publish .

# Run as a normal user instead of root, which is safer if the app is ever attacked.
USER $APP_UID

# .NET container images listen on port 8080 by default.
EXPOSE 8080

# The command that starts the app when a container starts.
ENTRYPOINT ["dotnet", "BankApi.dll"]