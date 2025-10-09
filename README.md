# Pokémon 3D – GameJolt Edition (Client)

Custom build of Pokémon 3D (v0.59.3) with full **GameJolt login**, **encrypted credentials**, and **online server integration**.

This client connects directly to the new `P3D-Server` for online multiplayer sessions, using verified GameJolt accounts and persistent cloud saves.

---

## ✨ Features

- 🔐 **GameJolt Account Login**  
  Secure login system with encrypted token storage (`gamejoltAcc.dat`).
- 💾 **Online Save Integration**  
  Player saves are linked to GameJolt accounts for persistence across sessions.
- 🧭 **Server Browser**  
  Browse, refresh, and connect to servers through an in-game interface.
- 🚀 **Instant Redirect**  
  After successful login, the client jumps straight to the server selection screen.
- ⚙️ **Offline/Bypass Mode**  
  Use “Bypass” to skip GameJolt login and test offline worlds.
- 📜 **Banlist + Version Check**  
  Client automatically fetches global banlist and version data from GitHub.
- 🔒 **Local Encryption**  
  All credentials stored locally are Base64 + AES encrypted for privacy.

---

## 🧩 Installation

### Prerequisites
- Windows 10 or later
- .NET Framework 4.8+
- XNA/MonoGame runtime

### Steps
1. Clone or download the repo:
   ```bash
   git clone https://github.com/<yourusername>/P3D-Client.git
   cd P3D-Client
   ```
2. Open the solution in **Visual Studio 2022** (VB.NET project).
3. Build the project (Debug or Release).
4. Run the game from:
   ```
   bin/Release/P3D-Client.exe
   ```

---

## 🔗 Connecting to Server

1. Launch the client.
2. Log in using your **GameJolt username** and **token**.
3. After successful login, select your target server from the **Join Server Screen**.
4. Your online save will load automatically.

> ⚠️ The server must be running and publicly reachable.  
> If using localhost, start the server first (`P3D-Server.exe`).

---

## ⚙️ Configuration Files

| File | Description |
|------|--------------|
| `Save/gamejoltAcc.dat` | Encrypted local credentials |
| `Save/server_list.dat` | Saved server list |
| `Logs/` | Game logs and crash reports |
| `Core/StringObfuscation.vb` | Handles Base64 and encryption/decryption |

---

## 🧠 Development Notes

- The login UI is defined in `P3D/Network/GameJolt/LogInScreen.vb`.
- GameJolt constants and API URLs are stored in `P3D/Network/GameJolt/Classified.vb`.
- The `JoinServerScreen.vb` handles connection logic and online checks.
- Default Game ID / Key for localhost testing is defined in `Classified.vb`:
  ```vb
  GameJolt_Game_ID = "MTIz" ' "123"
  GameJolt_Game_Key = "bG9jYWwtdGVzdC1rZXk=" ' "local-test-key"
  ```

---

## 🧑‍💻 Authors

- **AirysDark** – Core developer / reverse engineer  
- Based on **Pokémon 3D (Kolben Games)** legacy engine

---

## ⚖️ License

This project is for educational and archival purposes only.  
All Pokémon assets are © Nintendo / Game Freak.  
This fork is **non-commercial** and not affiliated with Kolben Games or GameJolt.
