FROM mcr.microsoft.com/dotnet/aspnet:8.0

ENV DEBIAN_FRONTEND=noninteractive

RUN apt-get update && \
    apt-get install -y python3 python3-pip tzdata && \
    apt-get clean

RUN pip3 install streamlink --break-system-packages

WORKDIR /app
COPY bin/Release/net8.0/publish  ./

ENTRYPOINT ["dotnet", "LazyDan2.dll", "--urls", "http://+:8080"]