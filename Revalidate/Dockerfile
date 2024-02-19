FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH=x64
ARG APPNAME=Revalidate
WORKDIR /src

# copy csproj and restore as distinct layers
COPY $APPNAME/*.csproj $APPNAME/
COPY $APPNAME.Generators/*.csproj $APPNAME.Generators/
RUN dotnet restore $APPNAME/$APPNAME.csproj -a $TARGETARCH

# copy and publish app and libraries
COPY $APPNAME/ $APPNAME/
COPY $APPNAME.Generators/ $APPNAME.Generators/
WORKDIR /src/$APPNAME
RUN dotnet publish -c $BUILD_CONFIGURATION -a $TARGETARCH -o /app --no-restore --self-contained


# final stage/image
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-alpine
EXPOSE 8080
EXPOSE 8081
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["./ReplayValidationAPI"]