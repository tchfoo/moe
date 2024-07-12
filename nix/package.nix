{ buildDotnetModule
, lib
, version
, dotnet-sdk_8
, dotnet-runtime_8
}:

buildDotnetModule {
  pname = "moe";
  inherit version;

  src = ../.;

  # to update, run `nix/update.sh` from the root of this repo
  nugetDeps = ./deps.nix;

  projectFile = [ "moe.csproj" ];

  dotnet-sdk = dotnet-sdk_8;
  dotnet-runtime = dotnet-runtime_8;

  executables = [ "moe" ];

  meta = with lib; {
    description = "A multi-purpose Discord bot made using Discord.Net";
    homepage = "https://github.com/ymstnt/moe/";
    license = licenses.gpl3;
    maintainers = with maintainers; [ gepbird ];
    platforms = platforms.all;
    mainProgram = "moe";
  };
}
