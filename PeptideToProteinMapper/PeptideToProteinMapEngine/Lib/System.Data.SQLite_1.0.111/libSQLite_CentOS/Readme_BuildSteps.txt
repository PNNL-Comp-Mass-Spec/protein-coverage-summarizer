# Build the container
sudo docker build -t "alchemistmatt:sqlitecentos" .

# Start the container
sudo docker run -v $PWD/data:/data:rw -v $PWD/parameters:/parameters:rw -it alchemistmatt:sqlitecentos /bin/bash

# In the container, copy the SQLite Interop file to /data
cp /app/libSQLite.Interop.so /data
