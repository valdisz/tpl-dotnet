FROM microsoft/dotnet:2.1-sdk-alpine AS build
WORKDIR /svc

# Copy csproj and restore as distinct layers
COPY ./src/*.csproj .
RUN dotnet restore

# Copy everything else and build
COPY ./src/* ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM docker.sable.lv/sable/img-dotnet-runtime:master
COPY --from=build /svc/out/* ./
