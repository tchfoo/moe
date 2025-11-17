{
  description = "A multi-purpose Discord bot made using Discord.Net.";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    # imports nixpkgs which is bad for performance
    # uses lib, fetchurl, dotnetCorePackages, zip
    nuget-packageslock2nix = {
      url = "github:mdarocha/nuget-packageslock2nix/main";
      inputs.nixpkgs.follows = "nixpkgs";
    };
  };

  outputs =
    inputs:
    with inputs;
    let
      supportedSystems = [
        "x86_64-linux"
        "aarch64-linux"
      ];
      withPkgs =
        f:
        nixpkgs.lib.genAttrs supportedSystems (
          system:
          f (
            import nixpkgs {
              inherit system;
              overlays = [ self.outputs.overlays.default ];
            }
          )
        );
    in
    {
      nixosModules.default = import ./nix/module.nix self.outputs.overlays.default;

      overlays.default = final: prev: {
        moe-dotnet = prev.callPackage ./nix/package.nix {
          inherit nuget-packageslock2nix;
          version = builtins.substring 0 8 self.lastModifiedDate or "dirty";
        };
      };

      packages = withPkgs (pkgs: {
        default = pkgs.moe-dotnet;
      });

      devShells = withPkgs (pkgs: {
        default = pkgs.mkShell {
          packages = with pkgs; [
            dotnet-sdk_10
            omnisharp-roslyn
          ];
        };
      });
    };
}
