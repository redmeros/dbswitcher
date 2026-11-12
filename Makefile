PROJECT := DBSwitcher/DBSwitcher.csproj
CONFIG  ?= Release
UNC     ?= //ad03/DCS/02-DCS-DESIGN/DCS-STAL/ASDBSWITCHER_NEW

.PHONY: run build release

run:
	dotnet run --project $(PROJECT)

build:
	dotnet build DBSwitcher.sln -c $(CONFIG)

# Publishes a self-contained single-file x64 build (no .NET runtime needed on the target machine)
release:
	dotnet publish $(PROJECT) -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$(UNC)"
