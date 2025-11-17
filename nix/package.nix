{
  lib,
  buildDotnetModule,
  stdenv,
  dotnet-sdk_10,
  dotnet-runtime_10,

  nuget-packageslock2nix,
  version,
}:

buildDotnetModule {
  pname = "moe-dotnet";
  inherit version;

  src = ../.;

  nugetDeps = nuget-packageslock2nix.lib {
    inherit (stdenv.hostPlatform) system;
    lockfiles = [ ../packages.lock.json ];
  };

  projectFile = [ "moe.csproj" ];

  dotnet-sdk = dotnet-sdk_10;
  dotnet-runtime = dotnet-runtime_10;

  executables = [ "moe" ];

  meta = {
    description = "A multi-purpose Discord bot made using Discord.Net";
    homepage = "https://github.com/tchfoo/moe";
    license = lib.licenses.gpl3;
    maintainers = with lib.maintainers; [ gepbird ];
    platforms = lib.platforms.all;
    mainProgram = "moe";
  };
}
