FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine as builder

COPY ./src /src
COPY ./Tesseract.sln ./Tesseract.sln
COPY ./common.props ./common.props
COPY ./houseofcat.png ./houseofcat.png
COPY ./version.props ./version.props

WORKDIR /
RUN dotnet restore && dotnet build --configuration Release

FROM builder as packager

RUN mkdir /package
WORKDIR /src
CMD \
dotnet pack -o /package -c Release HouseofCat.Compression/HouseofCat.Compression.csproj && \
dotnet pack -o /package -c Release HouseofCat.Dataflows/HouseofCat.Dataflows.csproj && \
dotnet pack -o /package -c Release HouseofCat.Dataflows.Pipelines/HouseofCat.Dataflows.Pipelines.csproj && \
dotnet pack -o /package -c Release HouseofCat.Encryption/HouseofCat.Encryption.csproj && \
dotnet pack -o /package -c Release HouseofCat.Extensions/HouseofCat.Extensions.csproj && \
dotnet pack -o /package -c Release HouseofCat.Logger/HouseofCat.Logger.csproj && \
dotnet pack -o /package -c Release HouseofCat.Metrics/HouseofCat.Metrics.csproj && \
dotnet pack -o /package -c Release HouseofCat.RabbitMQ/HouseofCat.RabbitMQ.csproj && \
dotnet pack -o /package -c Release HouseofCat.RabbitMQ.Dataflows/HouseofCat.RabbitMQ.Dataflows.csproj && \
dotnet pack -o /package -c Release HouseofCat.RabbitMQ.Pipelines/HouseofCat.RabbitMQ.Pipelines.csproj && \
dotnet pack -o /package -c Release HouseofCat.RabbitMQ.Services/HouseofCat.RabbitMQ.Services.csproj && \
dotnet pack -o /package -c Release HouseofCat.RabbitMQ.WorkState/HouseofCat.RabbitMQ.WorkState.csproj && \
dotnet pack -o /package -c Release HouseofCat.Reflection/HouseofCat.Reflection.csproj && \
dotnet pack -o /package -c Release HouseofCat.Serialization/HouseofCat.Serialization.csproj && \
dotnet pack -o /package -c Release HouseofCat.Utilities/HouseofCat.Utilities.csproj
