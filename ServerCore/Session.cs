using System.Net.Sockets;

namespace ServerCore;

public abstract class Session
{
    private Socket _socket;
    private SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();
    private SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();

    private RecvBuffer _recvBuffer = new RecvBuffer(65535);
    private Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
    private List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>(); 

    private bool _isConnected = true;
    private object _lock = new object();
    
    public void Start(Socket acceptSocket)
    {
        _socket = acceptSocket;
        
        _recvArgs.Completed += OnRecvCompleted;
        _sendArgs.Completed += OnSendCompleted;

        RegisterRecv();
    }
    
    public void Send(ArraySegment<byte> buffer)
    {
        if (buffer.Count == 0)
            return;

        lock (_lock)
        {
            _sendQueue.Enqueue(buffer);
            if (_pendingList.Count == 0)
                RegisterSend();
        }
    }

    private void RegisterRecv()
    {
        if (_isConnected == false)
            throw new Exception("Not connected");

        _recvBuffer.Clean();
        ArraySegment<byte> segment = _recvBuffer.WriteSegment;
        _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

        try
        {
            bool pending = _socket.ReceiveAsync(_recvArgs);
            if (!pending)
                OnRecvCompleted(null, _recvArgs);
        }
        catch (Exception e)
        {
            throw new Exception(e.ToString());
        }
    }

    private void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
    {
        if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
        {
            try
            {
                if (_recvBuffer.OnWrite(args.BytesTransferred) == false)
                {
                    Disconnected();
                    return;
                }

                int processLen = OnRecv(_recvBuffer.ReadSegment);
                if (processLen < 0 || _recvBuffer.DataSize < processLen)
                {
                    Disconnected();
                    return;
                }

                if (_recvBuffer.OnRead(processLen) == false)
                {
                    Disconnected();
                    return;
                }

                RegisterRecv();
            }
            catch (Exception e)
            {
                throw new Exception(e.ToString());
            }
        }
        else
        {
            Disconnected();
        }
    }

    private void RegisterSend()
    {
        if (_isConnected == false)
            return;

        while (_sendQueue.Count > 0)
        {
            ArraySegment<byte> buffer = _sendQueue.Dequeue();
            _pendingList.Add(buffer);
        }
        _sendArgs.BufferList = _pendingList;

        try
        {
            bool pending = _socket.SendAsync(_sendArgs);
            if (!pending)
                OnSendCompleted(null, _sendArgs);
        }
        catch (Exception e)
        {
            throw new Exception(e.ToString());
        }
    }

    private void OnSendCompleted(object sender, SocketAsyncEventArgs args)
    {
        lock (_lock)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                try
                {
                    _sendArgs.BufferList = null;
                    _pendingList.Clear();
                    if (_sendQueue.Count > 0)
                        RegisterSend();
                }
                catch (Exception e)
                {
                    throw new Exception(e.ToString());
                }
            }
            else
            {
                Disconnected();
            }
        }
    }

    private void Disconnected()
    {
        if (Interlocked.Exchange(ref _isConnected, false) == false)
            throw new Exception("Still connected");

        // OnDisconnected();
        _socket.Shutdown(SocketShutdown.Both);
        _socket.Close();
    }

    public abstract void OnConnected();
    public abstract void OnDisconnected();
    public abstract int OnRecv(ArraySegment<byte> segment);
    public abstract void OnSend();

}