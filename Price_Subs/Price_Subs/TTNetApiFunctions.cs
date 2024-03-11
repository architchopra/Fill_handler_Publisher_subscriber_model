using Ase_Qouted;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tt_net_sdk;

namespace Price_Subs
{
    public  class TTNetApiFunctions
    {
        private TTAPI m_api = null;
       
        private tt_net_sdk.WorkerDispatcher m_disp = null;
        PriceUpdateService updateService = null;
        Instrument_look_up ins_l = null;
        PriceSubscriber sub1 = null;
        CancellationTokenSource cancellationTokenSourcenew1 = new CancellationTokenSource();
        public Instrument instrument = null;

        public void Start(tt_net_sdk.TTAPIOptions apiConfig)
        {
            m_disp = tt_net_sdk.Dispatcher.AttachWorkerDispatcher();
            m_disp.DispatchAction(() =>
            {
                Init(apiConfig);
            });

            m_disp.Run();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Initialize the API </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public void Init(tt_net_sdk.TTAPIOptions apiConfig)
        {
            ApiInitializeHandler apiInitializeHandler = new ApiInitializeHandler(ttNetApiInitHandler);
            TTAPI.ShutdownCompleted += TTAPI_ShutdownCompleted;
            TTAPI.CreateTTAPI(tt_net_sdk.Dispatcher.Current, apiConfig, apiInitializeHandler);
        }

        public void ttNetApiInitHandler(TTAPI api, ApiCreationException ex)
        {
            if (ex == null)
            {
                Console.WriteLine("TT.NET SDK Initialization Complete");

                // Authenticate your credentials
                m_api = api;
                m_api.TTAPIStatusUpdate += new EventHandler<TTAPIStatusUpdateEventArgs>(m_api_TTAPIStatusUpdate);
                m_api.Start();
            }
            else if (ex.IsRecoverable)
            {
                // this is in informational update from the SDK
                Console.WriteLine("TT.NET SDK Initialization Message: {0}", ex.Message);
                if (ex.Code == ApiCreationException.ApiCreationError.NewAPIVersionAvailable)
                {
                    // a newer version of the SDK is available - notify someone to upgrade
                }
            }
            else
            {
                Console.WriteLine("TT.NET SDK Initialization Failed: {0}", ex.Message);
                if (ex.Code == ApiCreationException.ApiCreationError.NewAPIVersionRequired)
                {
                    // do something to upgrade the SDK package since it will not start until it is upgraded 
                    // to the minimum version noted in the exception message
                }
                Dispose();
            }
        }

        public void m_api_TTAPIStatusUpdate(object sender, TTAPIStatusUpdateEventArgs e)
        {
            Console.WriteLine("TTAPIStatusUpdate: {0}", e);
            if (e.IsReady == false)
            {
                // TODO: Do any connection lost processing here
                return;
            }
          
            Console.WriteLine("TT.NET SDK Authenticated");
            updateService = new PriceUpdateService(Dispatcher.Current);

            Instrument_look();
           
        }
        public async Task Instrument_look()
        {
            // Create an instance of Instrument_look_up
            ins_l = new Instrument_look_up(m_disp, MarketId.CME, ProductType.Future, "SR3", "SR3 Dec24",updateService);

            // Start the instrument lookup process asynchronously
            ins_l.Start();

            // Wait for the instrument lookup operation to complete
         

            // Create a PriceSubscriber for the obtained instrument
            
        }

        public void HandlePriceUpdate(PriceSubscriber subscriber, CancellationToken tk)
        {
            try
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
            catch (OperationCanceledException)
            {
                // Unsubscribe from price updates when cancellation is requested
                updateService.Unsubscribe(instrument, sub1);
                Console.WriteLine("exited");
            }
        }

        public void Dispose()
        {
          
            TTAPI.Shutdown();
        }

       
        public void TTAPI_ShutdownCompleted(object sender, EventArgs e)
        {
            Console.WriteLine("TTAPI Shutdown completed");
        }
    }
}
