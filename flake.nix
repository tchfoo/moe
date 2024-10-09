{
  description = "A multi-purpose Discord bot made using Discord.Net.";

  inputs = {
    # TODO: unpin when fixed: https://github.com/NixOS/nixpkgs/issues/347310
    nixpkgs.url = "github:NixOS/nixpkgs/7402aa90cff52a03f14e680346fa4038a1e17e93";
    flake-utils.url = "github:numtide/flake-utils";
    nuget-packageslock2nix = {
      url = "github:mdarocha/nuget-packageslock2nix/main";
      inputs.nixpkgs.follows = "nixpkgs";
    };
  };

  outputs =
    inputs:
    with inputs;
    {
      nixosModule = import ./nix/module.nix self.outputs.packages;
    }
    //
      flake-utils.lib.eachSystem
        [
          "x86_64-linux"
          "aarch64-linux"
        ]
        (
          system:
          let
            pkgs = import nixpkgs { inherit system; };
            version = builtins.substring 0 8 self.lastModifiedDate or "dirty";
          in
          {
            packages.default = pkgs.callPackage ./nix/package.nix { inherit version nuget-packageslock2nix; };
            devShells.default = pkgs.mkShell { packages = with pkgs; [ dotnet-sdk_8 ]; };
            formatter = pkgs.nixfmt-rfc-style;
          }
        );
}
