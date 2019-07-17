FROM centos:latest
MAINTAINER Matthew Monroe <monroem@gmail.com>

# RUN yum -y update
RUN yum -y install wget
RUN yum -y install unzip
RUN yum -y install gcc gcc-c++ make openssl-devel
RUN yum -y install less

RUN rpm --import "https://keyserver.ubuntu.com/pks/lookup?op=get&search=0x3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF"
RUN su -c 'curl https://download.mono-project.com/repo/centos7-stable.repo | tee /etc/yum.repos.d/mono-centos7-stable.repo'
RUN yum -y install mono-devel
RUN yum -y install mono-basic

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
