# SubscriberAdmin

```ps1
$token = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwianRpIjoiMDgxZTYzOGYtMWIwNC00ZmZjLWIyOTAtMjdhNDRlZjUzMDk5IiwibmJmIjoxNjQ4NzQ2NTM0LCJleHAiOjE2NDg3NDgzMzQsImlhdCI6MTY0ODc0NjUzNCwiaXNzIjoiU3Vic2NyaWJlckFkbWluIn0.CeMFKMWMio1vdf1ESPEPz7eAEY4HjSQscfRZziGR0sw'
curl.exe -s --oauth2-bearer $token https://localhost:7133/my | jq -C
```
