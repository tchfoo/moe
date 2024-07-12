{
  description = "A multi-purpose Discord bot made using Discord.Net.";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = inputs: with inputs;
    {
      nixosModule = import ./nix/module.nix self.outputs.packages;
    } //
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = import nixpkgs { inherit system; };
        version = builtins.substring 0 8 self.lastModifiedDate or "dirty";
      in
      {
        packages.default = pkgs.callPackage ./nix/package.nix { inherit version; };
        devShells.default = pkgs.mkShell {
          packages = with pkgs; [
            dotnet-sdk_8
          ];
        };
      });
}
