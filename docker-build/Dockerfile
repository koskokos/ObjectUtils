FROM microsoft/dotnet:2.2-sdk AS build-env

WORKDIR /build

COPY ./MiscellaneousUtils/MiscellaneousUtils.csproj /build/MiscellaneousUtils/MiscellaneousUtils.csproj
COPY ./MiscellaneousUtils.Tests/MiscellaneousUtils.Tests.csproj /build/MiscellaneousUtils.Tests/MiscellaneousUtils.Tests.csproj

RUN dotnet restore ./MiscellaneousUtils.Tests/MiscellaneousUtils.Tests.csproj

ARG Version

COPY . /build

RUN dotnet build -c Release -p:Version=${Version} ./MiscellaneousUtils.sln

RUN dotnet test ./MiscellaneousUtils.Tests/MiscellaneousUtils.Tests.csproj --no-build -c Release

RUN dotnet pack ./MiscellaneousUtils/MiscellaneousUtils.csproj --no-build -c Release -o /out -p:NuGetVersion=${Version}

ARG NuGetSource

ARG NuGetApiKey

RUN dotnet nuget push -s ${NuGetSource} -k ${NuGetApiKey} /out/MiscellaneousUtils.${Version}.nupkg