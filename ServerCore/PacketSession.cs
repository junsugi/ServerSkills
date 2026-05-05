using ServerCore;

namespace ServerSkills;

public abstract class PacketSession : Session
{
    public sealed override int OnRecv(ArraySegment<byte> buffer)
    {
        int processLength = 0;

        while (true)
        {
            if (buffer.Count < 4)
                break;
            
            // 헤더에 있는 총 데이터 크기 읽어오기
            ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            if (buffer.Count < dataSize)
                break;
            
            OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));

            processLength += dataSize;
            buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);
        }
        
        return processLength;
    }
    
    public abstract void OnRecvPacket(ArraySegment<byte> buffer);
}