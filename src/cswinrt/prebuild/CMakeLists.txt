cmake_minimum_required(VERSION 3.16)

project(cswinrt-prebuild LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED True)

if(NOT DEFINED CSWINRT_BUILD_VERSION)
    set(CSWINRT_BUILD_VERSION "0.0.0-private.0")
endif()

set(PREBUILD_SRCS
    main.cpp
    pch.h
)
add_executable(cswinrt-prebuild ${PREBUILD_SRCS})
target_compile_definitions(cswinrt-prebuild PRIVATE CSWINRT_VERSION_STRING="${CSWINRT_BUILD_VERSION}")
target_include_directories(cswinrt-prebuild PRIVATE ../  ${CMAKE_CURRENT_SOURCE_DIR}/../)

if(CMAKE_PROJECT_NAME STREQUAL PROJECT_NAME)
    install(TARGETS cswinrt-prebuild)
endif()
