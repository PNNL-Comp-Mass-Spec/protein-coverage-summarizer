# Build the container
sudo docker build -t "alchemistmatt:sqlitedebian" .

# Start the container
sudo docker run -v $PWD/data:/data:rw -v $PWD/parameters:/parameters:rw -it alchemistmatt:sqlitedebian /bin/bash

# In the container, copy the SQLite Interop file to /data
cp /app/libSQLite.Interop.so /data
