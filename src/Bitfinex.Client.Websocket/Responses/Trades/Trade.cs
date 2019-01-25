﻿using System;
using System.Linq;
using System.Reactive.Subjects;
using Bitfinex.Client.Websocket.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bitfinex.Client.Websocket.Responses.Trades
{
    /// <summary>
    /// Type of the trade
    /// </summary>
    public enum TradeType
    {
        Executed,
        UpdateExecution
    }

    /// <summary>
    /// The order that causes the trade determines if it is a buy or a sell.
    /// </summary>
    [JsonConverter(typeof(TradeConverter))]
    public class Trade : ResponseBase
    {
        /// <summary>
        /// Trade id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Millisecond time stamp
        /// </summary>
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTime Mts { get; set; }

        /// <summary>
        /// How much was bought (positive) or sold (negative).
        /// </summary>
        public double Amount { get; set; }

        /// <summary>
        /// Price at which the trade was executed
        /// </summary>
        public double Price { get; set; }

        /// <summary>
        /// Rate at which funding transaction occurred
        /// </summary>
        public double Rate { get; set; }

        /// <summary>
        /// Amount of time the funding transaction was for
        /// </summary>
        public double Period { get; set; }

        [JsonIgnore]
        public TradeType Type { get; set; }

        /// <summary>
        /// Target pair
        /// </summary>
        [JsonIgnore]
        public string Pair { get; set; }


        internal static void Handle(JToken token, SubscribedResponse subscription, Subject<Trade> subject)
        {
            var firstPosition = token[1];
            if (firstPosition.Type == JTokenType.Array)
            {
                // initial snapshot
                Handle(firstPosition.ToObject<Trade[]>(), subscription, subject);
                return;
            }

            var tradeType = TradeType.Executed;
            if (firstPosition.Type == JTokenType.String)
            {
                if((string)firstPosition == "tu")
                    tradeType = TradeType.UpdateExecution;
                else if((string)firstPosition == "hb")
                    return; // heartbeat, ignore
            }

            var data = token[2];
            if (data.Type != JTokenType.Array)
            {
                // bad format, ignore
                return; 
            }

            var trade = data.ToObject<Trade>();
            trade.Type = tradeType;
            trade.Pair = subscription.Pair;
            trade.ChanId = subscription.ChanId;
            subject.OnNext(trade);
        }

        internal static void Handle(Trade[] trades, SubscribedResponse subscription, Subject<Trade> subject)
        {
            var reversed = trades.Reverse().ToArray(); // newest last
            foreach (var trade in reversed)
            {
                trade.Type = TradeType.Executed;
                trade.Pair = subscription.Pair;
                trade.ChanId = subscription.ChanId;
                subject.OnNext(trade);
            }
        }
    }
}
