# 3.24 for DOWNLOAD_EXTRACT_TIMESTAMP; Debian 12 has 3.25.
cmake_minimum_required(VERSION 3.24)
project(CatchFishServer C CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

include(FetchContent)

# Plain HTTP is enough for a local or LAN server, so cpp-httplib is built without TLS and compression.
set(HTTPLIB_USE_OPENSSL_IF_AVAILABLE OFF CACHE BOOL "" FORCE)
set(HTTPLIB_USE_ZLIB_IF_AVAILABLE OFF CACHE BOOL "" FORCE)
set(HTTPLIB_USE_BROTLI_IF_AVAILABLE OFF CACHE BOOL "" FORCE)
set(HTTPLIB_INSTALL OFF CACHE BOOL "" FORCE)

FetchContent_Declare(httplib
    URL https://github.com/yhirose/cpp-httplib/archive/refs/tags/v0.18.3.tar.gz
    URL_HASH SHA256=a0567bcd6c3fe5cef1b329b96245119047f876b49e06cc129a36a7a8dffe173e
    DOWNLOAD_EXTRACT_TIMESTAMP TRUE)
FetchContent_Declare(json
    URL https://github.com/nlohmann/json/releases/download/v3.11.3/json.tar.xz
    URL_HASH SHA256=d6c65aca6b1ed68e7a182f4757257b107ae403032760ed6ef121c9d55e81757d
    DOWNLOAD_EXTRACT_TIMESTAMP TRUE)
# The amalgamation is one .c file, built below, so SQLite needs no install on any OS.
FetchContent_Declare(sqlite
    URL https://www.sqlite.org/2024/sqlite-amalgamation-3460100.zip
    URL_HASH SHA256=77823cb110929c2bcb0f5d48e4833b5c59a8a6e40cdea3936b99e199dbbe5784
    DOWNLOAD_EXTRACT_TIMESTAMP TRUE)
FetchContent_MakeAvailable(httplib json sqlite)

find_package(Threads REQUIRED)

add_library(sqlite3 STATIC ${sqlite_SOURCE_DIR}/sqlite3.c)
target_include_directories(sqlite3 PUBLIC ${sqlite_SOURCE_DIR})
target_link_libraries(sqlite3 PUBLIC Threads::Threads ${CMAKE_DL_LIBS})

add_executable(catchfish-server
    src/main.cpp
    src/GameRules.cpp
    src/Storage.cpp
    src/TimeFormat.cpp)
target_link_libraries(catchfish-server PRIVATE httplib::httplib nlohmann_json::nlohmann_json sqlite3)
target_compile_options(catchfish-server PRIVATE $<$<CXX_COMPILER_ID:GNU,Clang,AppleClang>:-Wall -Wextra>)

# Defaults that work from any working directory: the client's config and a database next to this file.
target_compile_definitions(catchfish-server PRIVATE
    CATCHFISH_DEFAULT_CONFIG="${CMAKE_CURRENT_SOURCE_DIR}/../Assets/CatchFish/Resources/LocalBackend/game_config.json"
    CATCHFISH_DEFAULT_DB="${CMAKE_CURRENT_SOURCE_DIR}/catchfish.db")
