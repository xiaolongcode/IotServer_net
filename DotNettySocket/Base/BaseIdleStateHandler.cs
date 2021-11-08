using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Coldairarrow.DotNettySocket.Base
{
    /// <summary>
    /// 
    /// </summary>
    public class BaseIdleStateHandler: IdleStateHandler
    {

        int ReaderIdleTimeSeconds;
        int WriterIdleTimeSeconds;
        int AllIdleTimeSeconds;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="readerIdleTimeSeconds"></param>
        /// <param name="writerIdleTimeSeconds"></param>
        /// <param name="allIdleTimeSeconds"></param>
        public BaseIdleStateHandler(int readerIdleTimeSeconds, int writerIdleTimeSeconds, int allIdleTimeSeconds) : base(readerIdleTimeSeconds, writerIdleTimeSeconds, allIdleTimeSeconds)
        {
            ReaderIdleTimeSeconds = readerIdleTimeSeconds;
            WriterIdleTimeSeconds = writerIdleTimeSeconds;
            AllIdleTimeSeconds = allIdleTimeSeconds;
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            base.ChannelRead(context, message);
        }
        public override void Read(IChannelHandlerContext context)
        {
            base.Read(context);
        }
        public override void ChannelRegistered(IChannelHandlerContext context)
        {
            base.ChannelRegistered(context);
        }
        public override void HandlerRemoved(IChannelHandlerContext context)
        {
            base.HandlerRemoved(context);

        }
        public override void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            if (evt is IdleStateEvent)
            {
               
            }
            base.UserEventTriggered(context, evt);
        }
        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            base.ChannelReadComplete(context);
        }
        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            base.ExceptionCaught(context, exception);
        }
    }
}
