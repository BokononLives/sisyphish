$headers = @{
    'Content-Type' = 'application/json';
    'Authorization' = "Bot ${Env:DISCORD_TOKEN}";
    'user-agent' = 'robot';
}

$body = '[
    {
        "name": "fish",
        "description": "fissshhh"
    }
]'

Invoke-WebRequest -Uri "https://discord.com/api/v10/applications/${Env:DISCORD_APPLICATION_ID}/commands" -Method PUT -Headers $headers -Body $body