FROM microsoft/dotnet:2.1-sdk-alpine AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ./src/*.csproj .
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/dotnet:2.1-aspnetcore-runtime-alpine
EXPOSE 5000
# HEALTHCHECK --interval=30s --timeout=10s --start-period=1m \
#     CMD curl -f http://localhost:5000/ping || exit 1
RUN addgroup -g 1001 -S appuser && \
    adduser -u 1001 -D -h /app -S -G appuser appuser
USER appuser
WORKDIR /app
COPY --from=build /app/src/out .
ENTRYPOINT ["dotnet", "svc.dll", "--urls", "http://*:5000"]
