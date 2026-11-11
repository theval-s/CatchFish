# The CatchFish server, built from source. From the repo root:
#   docker build -t catchfish-server Server
# Run: see "Docker" in Server/README.md.

FROM debian:12-slim AS build
RUN apt-get update \
    && apt-get install -y --no-install-recommends build-essential cmake ca-certificates \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /src
COPY CMakeLists.txt ./
COPY src src
RUN cmake -S . -B build -DCMAKE_BUILD_TYPE=Release \
    && cmake --build build -j"$(nproc)"

FROM debian:12-slim
RUN useradd --system --no-create-home catchfish \
    && mkdir /data \
    && chown catchfish /data
COPY --from=build /src/build/catchfish-server /usr/local/bin/
COPY content/game_config.json /etc/catchfish/game_config.json
USER catchfish
VOLUME /data
EXPOSE 8080
# 0.0.0.0 inside the container; publish the port on the host's 127.0.0.1 to keep it private.
# The seed config is only read while the database in /data has no config yet.
ENTRYPOINT ["catchfish-server", "--host", "0.0.0.0", "--config", "/etc/catchfish/game_config.json", "--db", "/data/catchfish.db"]
