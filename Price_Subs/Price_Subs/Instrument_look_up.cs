using Ase_Qouted;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using tt_net_sdk;

namespace Price_Subs
{
    public class Instrument_look_up
    {
        MarketId marketkey ;
        string m_product = "";
        ProductType m_prodType ;
        string m_alias = "";
        private tt_net_sdk.WorkerDispatcher m_disp = null;
        private InstrumentLookup m_instrLookupRequest = null;
        private InstrumentCatalog Catalog_Lookup = null;


        // Instrument object obtained from the lookup
        public List<Instrument> instrument { get; private set; }

        PriceUpdateService updateService = null;
        // TaskCompletionSource for managing the completion of the InstrumentTask

        CancellationTokenSource cancellationTokenSourcenew1 = new CancellationTokenSource();



        public Instrument_look_up( WorkerDispatcher disp,MarketId marketkey,ProductType prod_type, string prod, string alias,PriceUpdateService updateService) {
            this.m_disp = disp;
            this.marketkey = marketkey;
            this.m_product = prod;
            this.m_prodType= prod_type;
            this.m_alias = alias;
            this.updateService=updateService;
        }
        public void Start()
        {
            Catalog_Lookup = new InstrumentCatalog(marketkey, m_prodType, m_product, m_disp);
            Catalog_Lookup.OnData += new EventHandler<InstrumentCatalogEventArgs>(Catalog_OnData);
            Catalog_Lookup.GetAsync();
        }
      
        private void Catalog_OnData(object sender, InstrumentCatalogEventArgs e)
        {
            if (e.Event == ProductDataEvent.Found)
            {
                foreach (KeyValuePair<InstrumentKey, tt_net_sdk.Instrument> kvp in e.InstrumentCatalog.Instruments)
                {
                    string m_alias = kvp.Key.ToString();
                    m_alias = m_alias.Substring(4);
                    m_instrLookupRequest = new InstrumentLookup(m_disp, kvp.Key.MarketId, kvp.Value.Product.Type, kvp.Value.Product.Name, m_alias);
                    m_instrLookupRequest.OnData += m_instrLookupRequest_OnData;
                    m_instrLookupRequest.GetAsync();
                }
            }
            else
            {
                Console.WriteLine("Catalog Not Found - Event Code: " + e.Event);
            }
        }
        void m_instrLookupRequest_OnData(object sender, InstrumentLookupEventArgs e)
        {
            if (e.Event == ProductDataEvent.Found)
            {
                // Set the TaskCompletionSource result to indicate completion of the operation
                
                instrument.Add (e.InstrumentLookup.Instrument);
                Console.WriteLine("Found: {0}", instrument);

                 PriceSubscriber sub2 = new PriceSubscriber(e.InstrumentLookup.Instrument);

                // Subscribe to price updates for the instrument
                updateService.Subscribe(e.InstrumentLookup.Instrument, sub2);

                // Create and start a new thread to handle price updates
                Thread updateThread1 = new Thread(new ThreadStart(() => HandlePriceUpdate(sub2, cancellationTokenSourcenew1.Token)));
                updateThread1.Start();

            }
            else if (e.Event == ProductDataEvent.NotAllowed)
            {
                Console.WriteLine("Not Allowed : Please check your Token access");
            }
            else
            {
                // Instrument was not found and TT API has given up looking for it
                Console.WriteLine("Cannot find instrument: {0}", e.Message);
            }
        }
        public void HandlePriceUpdate(PriceSubscriber subscriber, CancellationToken tk)
        {
            
                // Obtain the instrument from the subscriber
                Instrument symbol = subscriber.Symbol();

                // Get the  price update publisher for the instrument.to be used o get the pric chaange notification
                PricePublisher publisher = subscriber.GetLatestPriceUpdatePublisher();

                // Get the event handle for price change notifications
                ManualResetEvent waitHandle = publisher.GetPriceChangedEvent();

                // Loop to handle price updates
                while (waitHandle.WaitOne())
                {
                    // Check for cancellation request
                    tk.ThrowIfCancellationRequested();

                    // Get the latest price update
                    PriceUpdate latestPriceUpdate = subscriber.GetLatestPriceUpdate();

                    // Get the current thread ID
                    int threadId = Thread.CurrentThread.ManagedThreadId;

                    // Output price update information
                    Console.WriteLine(threadId);
                    Console.WriteLine(latestPriceUpdate.e.Fields.GetBestBidPriceField(0));
                    Console.WriteLine(latestPriceUpdate.e.Fields.GetBestAskPriceField(0));
                    Console.WriteLine(latestPriceUpdate.e.Fields.GetBestAskQuantityField(0));
                    Console.WriteLine(latestPriceUpdate.e.Fields.GetBestBidQuantityField(0));

                    // Reset the event handle
                    waitHandle.Reset();
                }
            
           
        }
    }
}
