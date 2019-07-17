FROM mono:latest
MAINTAINER Matthew Monroe <monroem@gmail.com>

RUN apt-get -y update
RUN apt-get -y install wget
RUN apt-get -y install unzip
RUN apt-get -y install build-essential
RUN apt-get -y install less






WORKDIR /app/sqlite
RUN wget https://github.com/AshleyLab/motrpac-proteomics-pnnl-prototype/raw/master/step06/sqlite-netFx-full-source-1.0.111.0.zip
RUN unzip sqlite-netFx-full-source-1.0.111.0.zip

WORKDIR /app/sqlite/Setup
RUN chmod +x compile-interop-assembly-release.sh
RUN ./compile-interop-assembly-release.sh

WORKDIR ../bin/2013/Release/bin
RUN chmod -x libSQLite.Interop.so
RUN cp libSQLite.Interop.so /app/

WORKDIR /app/
