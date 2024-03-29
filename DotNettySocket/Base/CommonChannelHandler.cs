﻿using DotNetty.Transport.Channels;
using System;

namespace Coldairarrow.DotNettySocket
{
    class CommonChannelHandler : SimpleChannelInboundHandler<object>
    {
        public CommonChannelHandler(IChannelEvent channelEvent)
        {
            _channelEvent = channelEvent;
        }
        IChannelEvent _channelEvent { get; }

        protected override void ChannelRead0(IChannelHandlerContext ctx, object msg)
        {
            _channelEvent.OnChannelReceive(ctx, msg);
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            _channelEvent.OnChannelActive(context);
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            context.Flush();
        }
        public override void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            _channelEvent.UserEventTriggered(context, evt);
        }
        public override void ChannelInactive(IChannelHandlerContext context)
        {
            _channelEvent.OnChannelInactive(context.Channel);
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            _channelEvent.OnException(context.Channel, exception);
        }

    }
}