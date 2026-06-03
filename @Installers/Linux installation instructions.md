# Direct Linux Install

- Connect to the Linux server with your favorite SSH client. I'm using Putty.
- You can get the IP address of the Linux machine via ```ip addr show```

## Prerequisites
sudo apt update
sudo apt install -y unzip

## Download & Extract
# Download (update version as needed)
wget https://github.com/NTDLS/Katzebase/releases/download/0.37.0/Katzebase.linux.x64.zip
- or -
wget https://github.com/NTDLS/Katzebase/releases/download/0.38.0/Katzebase.linux.arm64.zip

# Extract to /opt
sudo unzip Katzebase.linux.x64.zip -d /opt/katzebase
- or -
sudo unzip Katzebase.linux.arm64.zip -d /opt/katzebase

# Make executable
sudo chmod +x /opt/katzebase/NTDLS.Katzebase.Server

## Create a Dedicated Service User

sudo useradd --system --no-create-home --shell /usr/sbin/nologin katzebase
sudo chown -R katzebase:katzebase /opt/katzebase

## Install the systemd Service
sudo nano /etc/systemd/system/katzebase.service

# Paste
[Unit]
Description=Katzebase Document Database Server
After=network.target

[Service]
Type=simple
User=katzebase
WorkingDirectory=/opt/katzebase
ExecStart=/opt/katzebase/NTDLS.Katzebase.Server
Restart=on-failure
RestartSec=5

[Install]
WantedBy=multi-user.target

## Enable and Start
sudo systemctl enable katzebase
sudo systemctl start katzebase

## Verify
sudo systemctl status katzebase
journalctl -u katzebase -f
