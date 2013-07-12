REM build for .NET framework v4.5 (aka v4.0.30319)
set msbuild=%windir%\Microsoft.NET\Framework\v4.0.30319\msbuild
%msbuild% Satsuma.sln /t:Rebuild /p:Configuration=Debug /p:Platform="Any CPU"
%msbuild% Satsuma.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
