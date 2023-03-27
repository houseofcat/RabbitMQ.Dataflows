FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine as builder

COPY ./src /src
COPY ./Tesseract.sln /Tesseract.sln
COPY ./common.props /common.props
COPY ./houseofcat.png /houseofcat.png
COPY ./version.props /version.props

WORKDIR /

RUN dotnet build --configuration Release

FROM builder as tests

COPY ./tests /tests
COPY ./wait-for.sh /wait-for.sh

WORKDIR /tests
RUN dotnet restore
CMD /wait-for.sh rabbitmq:5672 -t 30 -- \
    dotnet test --no-restore --blame-crash --logger "console;verbosity=detailed"
