#!/bin/bash

if [ -z "$1" ]; then
    echo "Missing project name"
    exit 1
fi

if [ -z "$2" ]; then
    echo "Missing environment name"
    exit 1
fi

if [ -z "$3" ]; then
    echo "Missing source directory"
    exit 1
fi

if [ -z "$4" ]; then
    echo "Missing entrypoint"
    exit 1
fi

PROJECTNAME="$1"
ENVIRONMENTNAME="$2"
USERNAME="$PROJECTNAME-$ENVIRONMENTNAME"
GROUPNAME="$PROJECTNAME-$ENVIRONMENTNAME"
SOURCEDIR=$(realpath "$3")
ENTRYPOINT="$4"
TARGETDIR="/var/$PROJECTNAME-$ENVIRONMENTNAME"
SERVICENAME="$PROJECTNAME-$ENVIRONMENTNAME.service"

echo "Installing application '$PROJECTNAME ($ENVIRONMENTNAME)' from '$SOURCEDIR'..."

# Ensure the group exists
if [ $(getent group "$GROUPNAME") ]; then
  echo "Group $GROUPNAME already exists."
else
  echo -n "Creating group '$GROUPNAME'... "
  groupadd "$GROUPNAME"
  echo "done"

fi

# Ensure the user exists
if id "$USERNAME" >/dev/null 2>&1; then
    echo "User $USERNAME already exists."
else
    echo -n "Creating user '$USERNAME'... "
    useradd --system -g "$GROUPNAME" "$USERNAME"
    echo "done"
fi

# Stopping service if it exist
if [ -f "/etc/systemd/system/$SERVICENAME" ]; then
    echo -n "Stopping service... "
    systemctl stop $SERVICENAME
    echo "done"
fi

# Updating the service file
echo -n "Updating service... "
cat << EOM > "/etc/systemd/system/$SERVICENAME"
[Unit]
Description=$PROJECTNAME ($ENVIRONMENTNAME)
After=network.target

[Service]
ExecStart=dotnet $SOURCEDIR/$ENTRYPOINT
WorkingDirectory=$SOURCEDIR
Restart=on-failure
User=$USERNAME
Group=$GROUPNAME
Environment="ASPNETCORE_ENVIRONMENT=Production"
Environment="ASPNETCORE_URLS=http://0.0.0.0:3000"

[Install]
WantedBy=multi-user.target
EOM
systemctl daemon-reload
echo "done"

# Sync files
echo -n "Updating files... "
rsync -a "$SOURCEDIR/" "/var/$FULLNAME"
chown -R $USERNAME:$GROUPNAME $SOURCEDIR
echo  "done"

# Starting the service
echo -n "Starting service... "
systemctl start $SERVICENAME
echo "done"
