# Recipes and $(shell) need a POSIX sh plus rm, tr and awk. cmd/PowerShell don't have them on PATH, Git for Windows does,
# so they are called by full path. 8.3 path because this make can't handle spaces in SHELL.
GIT_BIN ?= C:/PROGRA~1/Git/usr/bin
SHELL := $(GIT_BIN)/sh.exe

PROJECT := DBSwitcher/DBSwitcher.csproj
CONFIG  ?= Release
UNC     ?= //ad03/DCS/02-DCS-DESIGN/DCS-STAL/ASDBSWITCHER_NEW

.PHONY: run build clean version release

run:
	dotnet run --project $(PROJECT)

build:
	dotnet build DBSwitcher.sln -c $(CONFIG)

clean:
	dotnet clean DBSwitcher.sln -c Debug
	dotnet clean DBSwitcher.sln -c Release
	$(GIT_BIN)/rm.exe -rf DBSwitcher/bin DBSwitcher/obj

# Version of the last release is kept in the VERSION file (missing = nothing released yet).
# BUMP=patch (default) | minor | major selects which part release increments.
# VERSION=x.y.z overrides the computed version.
BUMP ?= patch
LAST := $(shell $(GIT_BIN)/tr.exe -d '[:space:]' < VERSION 2>/dev/null)
ifeq ($(LAST),)
NEXT := 1.0.0
else
NEXT := $(shell echo $(LAST) | $(GIT_BIN)/awk.exe -F. -v b=$(BUMP) 'BEGIN{OFS="."} {if(b=="major"){$$1++;$$2=0;$$3=0}else if(b=="minor"){$$2++;$$3=0}else{$$3++}; print}')
endif
VERSION ?= $(NEXT)

version:
	@$(GIT_BIN)/echo.exe last: $(if $(LAST),$(LAST),none)
	@$(GIT_BIN)/echo.exe next: $(VERSION)

# Bumps the version, then packs a Velopack release (installer, full and delta packages) into the UNC feed that installed apps update from.
# VERSION is written back only after the release succeeded. Commit the VERSION file afterwards.
# Needs: dotnet tool install -g vpk --version 1.2.161
release:
	dotnet publish $(PROJECT) -c Release -r win-x64 --self-contained true -p:Version=$(VERSION) -o publish
	vpk pack --packId DBSwitcher --packTitle ASDbSwitcher --packVersion $(VERSION) --packDir publish --mainExe DBSwitcher.exe --icon DBSwitcher/logo.ico --outputDir "$(UNC)"
	echo $(VERSION)> VERSION
