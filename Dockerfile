FROM mcr.microsoft.com/dotnet/sdk:6.0.403-bullseye-slim AS builder
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

WORKDIR /source
COPY PluginBuilder/. PluginBuilder/.

ARG CONFIGURATION_NAME=Release
ARG VERSION
ARG GIT_COMMIT
RUN cd PluginBuilder && dotnet publish -p:Version=${VERSION} -p:GitCommit=${GIT_COMMIT} --output /app/ --configuration ${CONFIGURATION_NAME}

FROM mcr.microsoft.com/dotnet/aspnet:6.0.11-bullseye-slim
ENV LC_ALL en_US.UTF-8
ENV LANG en_US.UTF-8
WORKDIR /datadir
WORKDIR /app
ENV PB_DATADIR=/datadir
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
VOLUME /datadir

ENV DEBIAN_FRONTEND=noninteractive 

# Install curl
RUN apt-get -qq update \
  && apt-get -qq install apt-transport-https ca-certificates curl gnupg lsb-release --no-install-recommends \
  && rm -rf /var/lib/apt/lists/*

# Install docker
RUN curl -o cli.deb https://download.docker.com/linux/debian/dists/bullseye/pool/stable/amd64/docker-ce-cli_24.0.1-1~debian.11~bullseye_amd64.deb && \
    curl -o buildx.deb https://download.docker.com/linux/debian/dists/bullseye/pool/stable/amd64/docker-buildx-plugin_0.10.4-1~debian.11~bullseye_amd64.deb && \
    dpkg -i cli.deb buildx.deb && \
    rm cli.deb buildx.deb

COPY --from=builder "/app" .
ENTRYPOINT ["/app/PluginBuilder"]
