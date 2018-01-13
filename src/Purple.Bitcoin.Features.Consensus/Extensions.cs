﻿using System;
using System.Threading.Tasks;
using Purple.Bitcoin.P2P.Protocol;
using Purple.Bitcoin.P2P.Protocol.Payloads;

namespace Purple.Bitcoin.Features.Consensus
{
    public static class Extensions
    {
        public static async Task<bool> IfPayloadIsAsync<TPayload>(this Message message, Func<TPayload, Task> action) where TPayload : Payload
        {
            TPayload payload = message.Payload as TPayload;

            if (payload == null)
                return await Task.FromResult(false);

            await action(payload).ConfigureAwait(false);

            return await Task.FromResult(true);
        }
    }
}
