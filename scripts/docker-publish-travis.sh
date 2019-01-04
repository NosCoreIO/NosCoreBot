DOCKER_ENV=''
DOCKER_TAG=''
DOCKER_REGISTRY='703970026174.dkr.ecr.us-west-2.amazonaws.com'

case "$TRAVIS_BRANCH" in
  "master")
    DOCKER_ENV=production
    DOCKER_TAG=latest
    ;;  
esac

pip install --user awscli

eval $(aws ecr get-login --region us-west-2 --no-include-email)
docker build -f ./src/NosCoreBot/dockerfile -t noscorebot:$DOCKER_TAG ./src/NosCoreBot --no-cache

docker tag noscorebot:$DOCKER_TAG $DOCKER_REGISTRY/noscorebot:$DOCKER_TAG

docker push $DOCKER_REGISTRY/noscorebot:$DOCKER_TAG