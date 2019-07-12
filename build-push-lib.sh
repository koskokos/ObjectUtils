set -ex
git pull
build=$((`cat .build`+1))
version=`cat .version`.$build
docker build --network=nuget-nw --build-arg GitHubNugetToken=$2 --build-arg Version=$version -f docker-build/Dockerfile ./src/
echo $build > .build
git add .build
git commit -m 'build increment' --author='kos-ci <koskokos.dev@gmail.com>'
git push
