# Direct Linux Install

- Connect to the Linux server with your favorite SSH client. I'm using Putty.
- You can get the IP address of the Linux machine via ```ip addr show```

- Install .net
```
sudo add-apt-repository ppa:dotnet/backports
sudo apt update
sudo apt-get install -y aspnetcore-runtime-9.0
```

- Install unzip
  > sudo apt install unzip

- Download Katzebase (be sure to change the URL for the version you want to install)
  > wget https://github.com/NTDLS/Katzebase/releases/download/0.33.0/Katzebase.linux.x64.zip

- Extract files
  > unzip Katzebase.linux.x64.zip -d Katzebase

- Make the File Executable
  > chmod +x ~/Katzebase/NTDLS.Katzebase.Server

- Execute the file
  > ~/Katzebase/NTDLS.Katzebase.Server

# Windows to Linux file copy using SSH

- Linux
  - Get the IP address for the Linux machine
    > ip addr show

- Windows
  - Install putty, connect to SSH with ip address from previous step.

- Linux
  - Configure SSH for password auth for file copy.
    > sudo nano /etc/ssh/sshd_config

  - Look for the following lines and ensure they are set:
    - "PasswordAuthentication yes"

  - Restart the ssh service.
      > sudo systemctl restart ssh

  - Install .net
    - >sudo add-apt-repository ppa:dotnet/backports
    - > sudo apt update
    - > sudo apt-get install -y aspnetcore-runtime-9.0
    - > (NOT NEEDED BUR DOCUMENTING HERE) sudo apt-get install -y dotnet-runtime-9.0 (THIS IS NT

- Linux
  - Install unzip
    > sudo apt install unzip

- Windows:
  - Copy package to the linux server.
    > scp .\output\Katzebase.linux.x64.zip josh@172.22.103.236:/home/josh

- Linux
  - Extract files
    > sudo unzip Katzebase.linux.x64.zip -d /opt/Katzebase

  - Make the File Executable
    > sudo chmod +x /opt/Katzebase/NTDLS.Katzebase.Server

  - Execute the file
    > sudo /opt/Katzebase/NTDLS.Katzebase.Server
