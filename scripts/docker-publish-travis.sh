DOCKER_ENV=''
DOCKER_TAG=''

case "$TRAVIS_BRANCH" in
  "master")
    DOCKER_ENV=production
    DOCKER_TAG=latest
    ;; 
esac

docker login -u $DOCKER_USERNAME -p $DOCKER_PASSWORD

docker build -f ./src/NosCoreBot/Dockerfile.$DOCKER_ENV -t noscorebot:$DOCKER_TAG ./src/NosCoreBot --no-cache

docker tag noscorebot:$DOCKER_TAG $DOCKER_USERNAME/noscorebot:$DOCKER_TAG

docker push $DOCKER_USERNAME/noscorebot:$DOCKER_TAG