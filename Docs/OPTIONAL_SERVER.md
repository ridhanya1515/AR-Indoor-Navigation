## 6. Run the full login + database + hardware workflow

The default review account works locally without a server. For database-backed authentication, connect the **Windows computer, phone and ESP32 to the same router**.

1. Install **Python 3.10 or newer** on Windows. The server needs no third-party Python packages.
2. Open **Server/CREATE_USER.bat**, enter a username, then a password of at least 10 characters. Password input is hidden.
3. Open **Server/START_SERVER.bat**. Keep the window open.
4. If Windows Firewall prompts, allow Python on your trusted **Private** network.
5. Open Command Prompt, type `ipconfig`, and find the Wi-Fi adapter's IPv4 address, for example `192.168.1.20`.
6. In the app's login screen, tick **Use optional laptop login server**, then set the server address to **http://192.168.1.20:8080**, replacing the example with your computer's actual address. Do not use `localhost` on the phone.
7. Enter the account you created and choose **Sign in and load map**.
8. In the ESP32 firmware, set `ROUTER_SSID` and `ROUTER_PASSWORD` to that router's network details. Upload again.
9. Read the ESP32's router address in Serial Monitor, for example `192.168.1.45`.
10. In app Settings, set the sensor endpoint to **http://192.168.1.45/status**, enter the matching key and enable it.
11. Select a room, align at a known checkpoint, start, then obstruct the monitored corridor to demonstrate automatic rerouting.

The server stores users, password hashes, expiring login sessions and the map in **Server/campus.db**. Passwords use salted PBKDF2-HMAC-SHA256; only hashes of session tokens are stored. Map access requires a valid session. The app keeps its token in memory and does not save your password.

This is a private-network demonstration server. HTTP exposes traffic on that network; use only demonstration credentials. For deployment with real accounts, use HTTPS with a trusted certificate, managed account provisioning and a maintained production server. `server.py --cert certificate.pem --key private-key.pem` supports TLS; the phone must trust the certificate. The app does not bypass certificate validation.

### Server commands, if the batch files cannot run

Open Command Prompt in the **Server** folder:

```text
py -3 server.py --add-user student
py -3 server.py
```

Run the first command once to create the user. Use your own password when prompted. If `py` is not installed but `python` is available, replace `py -3` with `python`.

