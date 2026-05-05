namespace ServerCore;

public class RecvBuffer
{
    private ArraySegment<byte> _buffer;
    private int _readPos;
    private int _writePos;
        
    // 생성자 초기화
    public RecvBuffer(int bufferSize)
    {
        _buffer = new ArraySegment<byte>(new byte[bufferSize]);
    }

    public int DataSize => _writePos - _readPos;
    public int FreeSize => _buffer.Count - _writePos;
    
    public ArraySegment<byte> ReadSegment => new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _readPos, DataSize);
    public ArraySegment<byte> WriteSegment => new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _writePos, FreeSize);
    
    
    // OnRead
    public bool OnRead(int numOfBytes)
    {
        if (numOfBytes > DataSize)
            return false;

        _readPos += numOfBytes;
        return true;
    }
    // OnWrite
    public bool OnWrite(int numOfBytes)
    {
        if (numOfBytes > FreeSize)
            return false;

        _writePos += numOfBytes;
        return true;
    }

    // Clean
    public void Clean()
    {
        // 복사 후 사용 (Race Condition)
        int dataSize = DataSize;
        if (dataSize == 0)
            _readPos = _writePos = 0;
        else
        {
            Array.Copy(_buffer.Array, _buffer.Offset + _readPos, _buffer.Array, _buffer.Offset, dataSize);
            _readPos = 0;
            _writePos = dataSize;
        }
    }
}