set /P build=<.build
set /P version=<.version
set version=%version%.%build%
cd src
set NuGetSource=%1
set NuGetApiKey=%2

dotnet build -c Release -p:Version=%version% ./MiscellaneousUtils.sln
dotnet test ./MiscellaneousUtils.Tests/MiscellaneousUtils.Tests.csproj --no-build -c Release
dotnet pack ./MiscellaneousUtils/MiscellaneousUtils.csproj --no-build -c Release -o /out -p:NuGetVersion=%version%

dotnet nuget push -s %NuGetSource% -k %NuGetApiKey% /out/MiscellaneousUtils.%version%.nupkg

REM echo $build > .build
REM git add .build
REM git commit -m 'build increment' --author='kos-ci <koskokos.dev@gmail.com>'
REM git push