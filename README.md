**Sisyphish v0.2**

- This project is intended to be serve as the interactions endpoint for a Discord Application.
- It doesn't have any commands yet, but it passes Discord's security validations.
- It relies on environment variables to work properly.
- Run refresh_secrets.ps1 AT YOUR OWN RISK. Any costs incurred are your responsibility. This script is intended to be run in your terminal BEFORE launching Visual Studio Code. It will set environment variables based on ALL of your Google Cloud secrets, scoped to that terminal.
- If you have a large number of secrets, I highly recommend implementing a different solution to store and retrieve these variables.