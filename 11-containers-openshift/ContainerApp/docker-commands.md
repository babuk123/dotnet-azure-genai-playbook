# Try it

```bash
# Option A: Dockerfile
docker build -t container-demo:1.0.0 .
# Option B: no Dockerfile (.NET SDK builds the image itself)
dotnet publish -c Release -t:PublishContainer

# Run with limits and config from environment variables
docker run -d --name demo -p 8080:8080 --cpus=1 --memory=256m \
  -e App__Greeting="Hello from env var" container-demo:1.0.0

curl localhost:8080/runtime        # ProcessorCount=1, GC heap limit ~192 MB (75% of 256m), User=app
curl localhost:8080/health/ready

# Graceful shutdown: start a slow request, then stop the container
curl localhost:8080/slow & docker stop -t 30 demo   # request finishes, logs show "draining"
docker logs demo

# Inspect the image
docker image ls container-demo
docker history container-demo:1.0.0
```
