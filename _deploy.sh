#!/bin/bash

# Configuration
SERVER_USER="roland"
SERVER_IP="192.168.0.195"
REMOTE_DIR="/usr/local/bin/my-gallery-api"
REMOTE_TMP_DIR="/tmp"
DIST_TAR="my-gallery-api.tar.gz"
SERVICE_NAME="my-gallery-api"

dotnet publish -c Release -o ./publish --runtime linux-x64 --self-contained

# Create a tarball of the dist directory
tar czf "$DIST_TAR" -C ./publish .

# Copy tarball to server
echo "Copy $DIST_TAR to $SERVER_USER@$SERVER_IP:/tmp/$DIST_TAR"
scp "$DIST_TAR" "$SERVER_USER@$SERVER_IP:/tmp/$DIST_TAR"

Extract tarball on server and move files to target directory
echo "Extract $DISTR_TAR"
ssh "$SERVER_USER@$SERVER_IP" "
    sudo rm -rf '$REMOTE_DIR'/*
    sudo tar xzf "/tmp/$DIST_TAR" -C "$REMOTE_DIR"
    sudo rm "/tmp/$DIST_TAR"
    sudo chmod o+x $REMOTE_DIR/$SERVICE_NAME
"

# Create systemd service file content
read -r -d '' SERVICE_FILE << EOM
[Unit]
Description=My Gallery API Service
After=network.target

[Service] 
User=$REMOTE_USER
WorkingDirectory=$REMOTE_DIR
ExecStart=$REMOTE_DIR/my-gallery-api
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_RUNNING_IN_CONTAINER=false
Environment=DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
Environment=DOTNET_USE_POLLING_FILE_WATCHER=true
Environment=ASPNETCORE_URLS=http://+:5001
SuccessExitStatus=143
Restart=on-failure
RestartSec=10

[Install]
WantedBy=multi-user.target
EOM

# Copy service file to remote server's home directory
echo "$SERVICE_FILE" | ssh "$SERVER_USER@$SERVER_IP" "cat > $REMOTE_TMP_DIR/$SERVICE_NAME.service"

# Move service file to systemd directory with sudo
ssh "$SERVER_USER@$SERVER_IP" "sudo mv $REMOTE_TMP_DIR/$SERVICE_NAME.service /etc/systemd/system/ && sudo chown root:root /etc/systemd/system/$SERVICE_NAME.service"

# Reload systemd, enable and restart service
ssh "$SERVER_USER@$SERVER_IP" "sudo systemctl daemon-reload && sudo systemctl enable $SERVICE_NAME && sudo systemctl restart $SERVICE_NAME"

# Remove local tarball
echo "Removing local $DIST_TAR"
rm "$DIST_TAR"
rm -r ./publish/*

echo "Deployment complete."

