# Debug phase
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Publish phase
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
ARG TARGETARCH
ARG APPNAME=Revalidate
WORKDIR /src

# Copy project file and restore as distinct layers
COPY --link $APPNAME/*.csproj $APPNAME/
COPY --link $APPNAME.Api/*.csproj $APPNAME.Api/
RUN dotnet restore -a $TARGETARCH $APPNAME/$APPNAME.csproj

# Copy source code and publish app
COPY --link $APPNAME/ $APPNAME/
COPY --link $APPNAME.Api/ $APPNAME.Api/

WORKDIR /src/$APPNAME
RUN dotnet publish -a $TARGETARCH --no-restore -o /app


# Final phase
FROM mcr.microsoft.com/dotnet/aspnet:10.0
EXPOSE 8080
EXPOSE 8081

# Install docker CLI
RUN apt-get update && \
    apt-get install -y docker.io && \
    rm -rf /var/lib/apt/lists/*

# RUN apk add --no-cache tzdata

WORKDIR /app
COPY --link --from=publish /app .

# Allow APP_UID to write to the data directories
RUN mkdir -p /app/data && \
    chown -R $APP_UID:$APP_UID /app/data && \
    chmod -R 777 /app/data # temporary 777 because there are issues with running Docker MSM

USER $APP_UID
ENTRYPOINT ["./Revalidate"]