using DotNetty.Buffers;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;

namespace Coldairarrow.DotNettySocket
{
    class TcpSocketServer : BaseTcpSocketServer<ITcpSocketServer, ITcpSocketConnection, byte[]>, ITcpSocketServer
    {
        public TcpSocketServer(int port, TcpSocketServerEvent<ITcpSocketServer, ITcpSocketConnection, byte[]> eventHandle)
            : base(port, eventHandle)
        {
        }

        public override void OnChannelReceive(IChannelHandlerContext ctx, object msg)
        {
            PackException(() =>
            {
                var bytes = (msg as IByteBuffer).ToArray();
                var theConnection = GetConnection(ctx.Channel);
                _eventHandle.OnRecieve?.Invoke(this, theConnection, bytes);
            });
        }
        public override void UserEventTriggered(IChannelHandlerContext ctx, object evt)
        {
            if (evt is IdleStateEvent)
            {
                if ((evt as IdleStateEvent).State == IdleState.ReaderIdle)
                {
                    ctx.CloseAsync();
                    System.Console.WriteLine("读超时");
                }
                if ((evt as IdleStateEvent).State == IdleState.WriterIdle)
                {
                    ctx.CloseAsync();
                    System.Console.WriteLine("写超时");
                }
                if ((evt as IdleStateEvent).State == IdleState.AllIdle)
                {
                    ctx.CloseAsync();
                    System.Console.WriteLine("全部超时");
                }
               
            }
        }
        protected override ITcpSocketConnection BuildConnection(IChannel clientChannel)
        {
            return new TcpSocketConnection(this, clientChannel, _eventHandle);
        }
    }
}
