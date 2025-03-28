# **Sisyphish v0.5**
- Sisyphish is a Discord bot for entertainment purposes.
- This project is intended to serve as the interactions endpoint to run Sisyphish as a hosted Discord Application.

---

## **Bot Guide (for Users):**

### Slash commands:
- **/fish** - Embark on a fishing expedition
- **/reset** - Delete all your fishing data **PERMANENTLY** and start from scratch. **NOT RECOMMENDED.**

---

## **Code Guide (for Developers):**

### **Prerequisites:**
- Create and configure a Discord Application and note the *ApplicationID*, *PublicKey* and *Token*.
- Create a Firestore collection in Google Cloud named "fishers" and note the **ProjectID*.
- Set up your permissions correctly in Google Cloud (e.g., IAM roles for the service account running your worker)

### **Before opening project:**
- This project relies on the following custom environment variables to run properly:
    - DISCORD_APPLICATION_ID
    - DISCORD_PUBLIC_KEY
    - DISCORD_TOKEN
    - GOOGLE_PROJECT_ID
    - GOOGLE_SERVICE_ACCOUNT
- Run *refresh_secrets.ps1* **AT YOUR OWN RISK**. Any costs incurred are your responsibility. This script is intended to be run in your terminal BEFORE launching Visual Studio Code, and is only useful if you intend to run this project locally. It will set environment variables based on ALL of your Google Cloud secrets, scoped to that terminal.
- The point of this script is to use secrets without storing them in a plain text (e.g., .env) file. If you have a large number of secrets, I highly recommend implementing a different solution to store and retrieve these variables.

### **Registering with Discord:**
- Perform the following one-time steps to finish setting up:
    - Enter the application URL as the interactions endpoint for your Discord Application
    - Run *register_commands.ps1* to set up bot commands