set -ex
git pull
build=$((`cat .build`+1))
version=`cat .version`.$build
docker build --network=nuget-nw --build-arg Version=$version --build-arg NuGetApiKey=$1 -f docker-build/Dockerfile ./src/
echo $build > .build
git add .build
git commit -m 'build increment' --author='kos-ci <koskokos.dev@gmail.com>'
git push
