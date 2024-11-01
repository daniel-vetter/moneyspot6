#!/bin/bash

# extract app files from the created image
mkdir -p ./build/var/moneyspot
TEMPCONTAINER=$(docker create dvetter/moneyspot6:$GITHUB_SHA)
docker cp $TEMPCONTAINER:/app/. ./build/var/moneyspot

# copy all files required for a dep package over the app files
cp -r ./src/build/package-content/. ./build/

# fix permissions and line encoding for install scripts
sed -i 's/\r$//' ./build/DEBIAN/prerm
sed -i 's/\r$//' ./build/DEBIAN/postinst
chmod +x ./build/DEBIAN/postinst
chmod +x ./build/DEBIAN/prerm

# build package
dpkg-deb --build ./build
mv build.deb moneyspot.deb