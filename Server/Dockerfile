# The CatchFish server, built from source. Build from the repo root, since the image also takes the
# game config from Assets/:
#   docker build -f Server/Dockerfile -t catchfish-server .
# Run: see "Docker" in Server/README.md.

FROM debian:12-slim AS build
RUN apt-get update \
    && apt-get install -y --no-install-recommends build-essential cmake ca-certificates \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /src
COPY Server/CMakeLists.txt Server/
COPY Server/src Server/src
RUN cmake -S Server -B Server/build -DCMAKE_BUILD_TYPE=Release \
    && cmake --build Server/build -j"$(nproc)"

FROM debian:12-slim
RUN useradd --system --no-create-home catchfish \
    && mkdir /data \
    && chown catchfish /data
COPY --from=build /src/Server/build/catchfish-server /usr/local/bin/
COPY Assets/CatchFish/Resources/LocalBackend/game_config.json /etc/catchfish/game_config.json
USER catchfish
VOLUME /data
EXPOSE 8080
# 0.0.0.0 inside the container; publish the port on the host's 127.0.0.1 to keep it private.
ENTRYPOINT ["catchfish-server", "--host", "0.0.0.0", "--config", "/etc/catchfish/game_config.json", "--db", "/data/catchfish.db"]
