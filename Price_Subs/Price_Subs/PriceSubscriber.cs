using Price_Subs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tt_net_sdk;

namespace Price_Subs
{
    public class PriceSubscriber
    {
        private ConcurrentDictionary<Instrument, PricePublisher> publishers = new ConcurrentDictionary<Instrument, PricePublisher>();
        public readonly Instrument symbol;
        public PriceSubscriber(Instrument symbol)
        {
            this.symbol = symbol;
        }

        public PriceUpdate GetLatestPriceUpdate()
        {

            if (publishers.TryGetValue(symbol, out PricePublisher pub))
            {
                //get latest price directly from the publisher
                return pub.GetLatestPriceUpdate();

            }
            else
            {
                return default(PriceUpdate);
            }

        }
        public PricePublisher GetLatestPriceUpdatePublisher()
        {


            return publishers[symbol];

        }
        public void Add(PricePublisher publisher)
        {
            // add a particular subscriber to the publisher 
            publishers.TryAdd(symbol, publisher);
        }
        public Instrument Symbol()
        {
            return symbol;
        }
        public void Dispose()
        {
            // Clean up resources, unsubscribe from publishers, etc.

            publishers.Clear();
        }

    }
}
