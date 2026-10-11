FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so the restore layer is cached until a package changes.
COPY src/Auth.Domain/Auth.Domain.csproj src/Auth.Domain/
COPY src/Auth.Application/Auth.Application.csproj src/Auth.Application/
COPY src/Auth.Infrastructure/Auth.Infrastructure.csproj src/Auth.Infrastructure/
COPY src/Auth.Api/Auth.Api.csproj src/Auth.Api/
RUN dotnet restore src/Auth.Api/Auth.Api.csproj

COPY src/ src/
RUN dotnet publish src/Auth.Api/Auth.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__Auth="Data Source=/data/auth.db"

# SQLite file lives on a volume; the non-root app user must own it.
RUN mkdir /data && chown app:app /data
VOLUME /data
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "Auth.Api.dll"]
