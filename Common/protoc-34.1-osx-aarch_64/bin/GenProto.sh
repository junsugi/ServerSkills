#!/bin/bash

# protoc 실행
protoc -I=./ --csharp_out=./ ./Protocol.proto
if [ $? -ne 0 ]; then
    echo "protoc failed"
    read -p "Press enter to continue..."
    exit 1
fi

# 파일 복사
cp -f Protocol.cs "../../../DummyClient/Packet/"
cp -f Protocol.cs "../../../Server/Packet/"
