# Debug phase
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Publish phase
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS publish
ARG TARGETARCH
ARG APPNAME=Revalidate
WORKDIR /src

# Copy project file and restore as distinct layers
COPY --link $APPNAME/*.csproj $APPNAME/
# COPY --link $APPNAME.MySubProject/*.csproj $APPNAME.MySubProject/
RUN dotnet restore -a $TARGETARCH $APPNAME/$APPNAME.csproj

# Copy source code and publish app
COPY --link $APPNAME/ $APPNAME/
# COPY --link $APPNAME.MySubProject/ $APPNAME.MySubProject/

WORKDIR /src/$APPNAME
RUN dotnet publish -a $TARGETARCH --no-restore -o /app


# Final phase
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
EXPOSE 8080
EXPOSE 8081

# RUN apk add --no-cache tzdata

WORKDIR /app
COPY --link --from=publish /app .
USER $APP_UID
ENTRYPOINT ["./Revalidate"]