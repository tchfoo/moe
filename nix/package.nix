{ buildDotnetModule
, fetchFromGitHub
, lib
}:

# to update deps.nix:
# checkout out github:gepbird/nixpkgs/moebot-fetch-deps
# update version number, rev and hash
# $ pkgs/tools/moebot/update.sh
# copy pkgs/tools/moebot/deps.nix to this directory
buildDotnetModule rec {
  pname = "moe";
  version = "2024-01-15.1";

  src = fetchFromGitHub {
    owner = "ymstnt";
    repo = pname;
    rev = "0368a158f95dbc33d91ce8a001cd645672e99679";
    hash = "sha256-0f0n4Wdp9Shddzc6oAq35me/rJ5qTbIAbg6mCGpYzts=";
  };

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
