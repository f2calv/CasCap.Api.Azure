#.NET Core Application environment variables
$env:ASPNETCORE_ENVIRONMENT = "Development"

docker-compose up --pull --build --remove-orphans
