I run the file in docker containers.

When changes have been made, redeploy (or rebuild, if needed), the API container.

if needed, you can run the following:
docker compose down api; docker compose build --no-cache api; docker compose up -d api

no "curl" commands, use "Invoke-Webrequest" (PS) instead.

keep changes to a minimum, if possible

aways verify your edits, make sure the application builds