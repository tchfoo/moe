{ buildDotnetModule
, lib
, version
}:

# to update deps.nix:
# checkout out github:gepbird/nixpkgs/moebot-fetch-deps
# $ pkgs/tools/moebot/update.sh
# copy pkgs/tools/moebot/deps.nix to this directory
buildDotnetModule {
  pname = "moe";
  inherit version;

  src = ../.;

  nugetDeps = ./deps.nix;

  projectFile = [ "moe.csproj" ];

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
