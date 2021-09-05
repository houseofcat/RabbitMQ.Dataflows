#!/usr/bin/env bash

error() {
  echo "Failed to build"
  docker-compose --project-name packager down
  docker-compose --project-name packager rm -f
  docker volume prune -f
  exit 1
}

trap error ERR

if [ -z "$GO_PIPELINE_COUNTER" ]; then
    export GO_PIPELINE_COUNTER=0
fi

if [ -z "$GO_STAGE_COUNTER" ]; then
    export GO_STAGE_COUNTER=0
fi

chmod -R 777 *.sh

echo =============================================================================
echo Packaging CTM.Quoting.Provider
echo =============================================================================

echo
echo =============================================================================
echo Packaging NuGet .nupkg file
echo =============================================================================

docker-compose --project-name packager up --build --exit-code-from package package
mkdir -p ./deployment
VERSION=$(xml sel -t -v "//Project/PropertyGroup/Version" version.props)
docker cp packager:/package/HouseofCat.Compression.${VERSION}.nupkg ./deployment
docker cp packager:/package/HouseofCat.Dataflows.${VERSION}.nupkg ./deployment
docker cp packager:/package/HouseofCat.Dataflows.Pipelines.${VERSION}.nupkg ./deployment
docker cp packager:/package/HouseofCat.Encryption.${VERSION}.nupkg ./deployment
docker cp packager:/package/HouseofCat.Extensions.${VERSION}.nupkg ./deployment
docker cp packager:/package/HouseofCat.Logger.${VERSION}.nupkg ./deployment
docker cp packager:/package/HouseofCat.Metrics.${VERSION}.nupkg ./deployment
docker cp packager:/package/HouseofCat.RabbitMQ.${VERSION}.nupkg ./deployment
docker cp packager:/package/HouseofCat.RabbitMQ.Dataflows.${VERSION}.nupkg ./deployment
docker cp packager:/package/HouseofCat.RabbitMQ.Pipelines.${VERSION}.nupkg ./deployment
docker cp packager:/package/HouseofCat.RabbitMQ.Services.${VERSION}.nupkg ./deployment
docker cp packager:/package/HouseofCat.RabbitMQ.WorkState.${VERSION}.nupkg ./deployment
docker cp packager:/package/HouseofCat.Reflection.${VERSION}.nupkg ./deployment
docker cp packager:/package/HouseofCat.Serialization.${VERSION}.nupkg ./deployment
docker cp packager:/package/HouseofCat.Utilities.${VERSION}.nupkg ./deployment
docker cp packager:/package/HouseofCat.Compression.${VERSION}.snupkg ./deployment
docker cp packager:/package/HouseofCat.Dataflows.${VERSION}.snupkg ./deployment
docker cp packager:/package/HouseofCat.Dataflows.Pipelines.${VERSION}.snupkg ./deployment
docker cp packager:/package/HouseofCat.Encryption.${VERSION}.snupkg ./deployment
docker cp packager:/package/HouseofCat.Extensions.${VERSION}.snupkg ./deployment
docker cp packager:/package/HouseofCat.Logger.${VERSION}.snupkg ./deployment
docker cp packager:/package/HouseofCat.Metrics.${VERSION}.snupkg ./deployment
docker cp packager:/package/HouseofCat.RabbitMQ.${VERSION}.snupkg ./deployment
docker cp packager:/package/HouseofCat.RabbitMQ.Dataflows.${VERSION}.snupkg ./deployment
docker cp packager:/package/HouseofCat.RabbitMQ.Pipelines.${VERSION}.snupkg ./deployment
docker cp packager:/package/HouseofCat.RabbitMQ.Services.${VERSION}.snupkg ./deployment
docker cp packager:/package/HouseofCat.RabbitMQ.WorkState.${VERSION}.snupkg ./deployment
docker cp packager:/package/HouseofCat.Reflection.${VERSION}.snupkg ./deployment
docker cp packager:/package/HouseofCat.Serialization.${VERSION}.snupkg ./deployment
docker cp packager:/package/HouseofCat.Utilities.${VERSION}.snupkg ./deployment

docker-compose --project-name packager down
docker-compose --project-name packager rm -f
docker volume prune -f

echo Done!
