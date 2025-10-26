{
  description = "A multi-purpose Discord bot made using Discord.Net.";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
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
      nixosModules.default = import ./nix/module.nix self.outputs.packages;

      overlays.default = final: prev: {
        moe-dotnet = prev.callPackage ./nix/package.nix {
          inherit nuget-packageslock2nix;
          version = builtins.substring 0 8 self.lastModifiedDate or "dirty";
        };
      };
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
            pkgs = import nixpkgs {
              inherit system;
              overlays = [ self.outputs.overlays.default ];
            };
          in
          {
            packages.default = pkgs.moe-dotnet;
            devShells.default = pkgs.mkShell {
              packages = with pkgs; [
                dotnet-sdk_8
                omnisharp-roslyn
              ];
            };
            formatter = pkgs.nixfmt-rfc-style;
          }
        );
}
