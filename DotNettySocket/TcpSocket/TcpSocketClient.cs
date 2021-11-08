using DotNetty.Buffers;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Coldairarrow.DotNettySocket
{
    class TcpSocketClient : BaseTcpSocketClient<ITcpSocketClient, byte[]>, ITcpSocketClient
    {
        public TcpSocketClient(string ip, int port, TcpSocketCientEvent<ITcpSocketClient, byte[]> clientEvent)
            : base(ip, port, clientEvent)
        {
        }

        public override void OnChannelReceive(IChannelHandlerContext ctx, object msg)
        {
            PackException(() =>
            {
                var bytes = (msg as IByteBuffer).ToArray();
                _clientEvent.OnRecieve?.Invoke(this, bytes);
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
        public async Task Send(byte[] bytes)
        {
            try
            {
                await _channel.WriteAndFlushAsync(Unpooled.WrappedBuffer(bytes));
                await Task.Run(() =>
                {
                    _clientEvent.OnSend?.Invoke(this, bytes);
                });
            }
            catch (Exception ex)
            {
                _clientEvent.OnException?.Invoke(ex);
            }
        }

        public async Task Send(string msgStr)
        {
            await Send(Encoding.UTF8.GetBytes(msgStr));
        }
    }
}
