#!/bin/sh
# build for .NET framework v4.0
xbuild Satsuma.sln /toolsversion:4.0 /t:Rebuild /p:Configuration=Debug /p:Platform="Any CPU"
xbuild Satsuma.sln /toolsversion:4.0 /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
