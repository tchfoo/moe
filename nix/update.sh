#!/bin/sh
nix build .#default.passthru.fetch-deps
./result nix/deps.nix
