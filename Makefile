# Portable build for gcc/clang (POSIX or Cygwin/MSYS). MSVC users should use build.bat.
CXX      ?= g++
CC       ?= gcc
CXXFLAGS ?= -O2 -std=c++17 -Wall -Wextra
CFLAGS   ?= -O2
LDFLAGS  ?= -lpthread

SRC_DIR  := src
OUT      := bin/Release/lvglConverter

all: $(OUT)

$(OUT): $(SRC_DIR)/lvgl_converter.cpp $(SRC_DIR)/png_decoder.c $(SRC_DIR)/png_decoder.h $(SRC_DIR)/stb_image.h
mkdir -p bin/Release
$(CXX) $(CXXFLAGS) $(SRC_DIR)/lvgl_converter.cpp $(SRC_DIR)/png_decoder.c -I$(SRC_DIR) -o $@ $(LDFLAGS)

clean:
rm -rf bin

.PHONY: all clean