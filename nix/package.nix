{
  buildDotnetModule,
  lib,
  system,
  version,
  dotnet-sdk_8,
  dotnet-runtime_8,
  nuget-packageslock2nix,
}:

buildDotnetModule {
  pname = "moe";
  inherit version;

  src = ../.;

  nugetDeps = nuget-packageslock2nix.lib {
    inherit system;
    lockfiles = [ ../packages.lock.json ];
  };

  projectFile = [ "moe.csproj" ];

  dotnet-sdk = dotnet-sdk_8;
  dotnet-runtime = dotnet-runtime_8;

  executables = [ "moe" ];

  meta = {
    description = "A multi-purpose Discord bot made using Discord.Net";
    homepage = "https://github.com/ymstnt/moe/";
    license = lib.licenses.gpl3;
    maintainers = with lib.maintainers; [ gepbird ];
    platforms = lib.platforms.all;
    mainProgram = "moe";
  };
}
