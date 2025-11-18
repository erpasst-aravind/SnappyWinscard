using SnappyWinscard;
using System;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using static GRGlibHF.MainWindow;
using TextBox = System.Windows.Controls.TextBox;

namespace GRGlibHF
{
    public partial class MainWindow : Window
    {
        public bool connActive = false;
        public byte[] SendBuff = new byte[263];
        public byte[] RecvBuff = new byte[263];
        public int SendLen, RecvLen, nBytesRet, reqType, Aprotocol, dwProtocol, cbPciLength;
        public Winscard.SCARD_READERSTATE RdrState;
        public Winscard.SCARD_IO_REQUEST pioSendRequest;
        private readonly object lock1 = new object();

        public MainWindow()
        {
            InitializeComponent();
            CardIo = new CardIo();
            this.DataContext = CardIo;
            BindingOperations.EnableCollectionSynchronization(CardIo.Devices, lock1);
            CardIo.ReaderStateChanged += CardIo_ReaderStateChanged;
            ConnectCard1();
            Task.Run(() => ReadCardsLoop());
        }

        public CardIo CardIo { get; set; }
        private bool _run = true;


        private void ReadCardsLoop()
        {
            while (_run)
            {
                try
                {
                    string cardUID = CardIo.GetCardUID();

                    if (!string.IsNullOrWhiteSpace(cardUID))
                    {
                        // Normalize UID
                        cardUID = cardUID.Replace("-", string.Empty).ToUpper();

                        // Skip if same card as last time
                        if (cardUID == _lastUid)
                        {
                            Thread.Sleep(200);
                            continue;
                        }

                        _lastUid = cardUID;

                        var retSt = calcDevNo(cardUID);

                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (retSt.value != null)
                            {
                                DisplayStatus($"Card-UID: {retSt.value}");
                                SendKeys.SendWait(retSt.value + "{ENTER}");
                            }
                            else
                            {
                                DisplayStatus();
                            }
                        });

                        Thread.Sleep(500); // throttle reads
                    }
                    else
                    {
                        // If no card present, clear the last UID so the next card is detected again
                        _lastUid = "";
                        Thread.Sleep(200);
                    }
                }
                catch
                {
                    Thread.Sleep(200);
                }
            }
        }


        private string _lastUid = "";

        private void ButtonGetUid_Click(object sender, RoutedEventArgs e)
        {
            retSts retSt = new retSts();
            string cardUID = string.Empty;
            cardUID = CardIo.GetCardUID();
            if (cardUID != null)
            {
                // Convert the first 4 bytes of the received UID to a hexadecimal string
                cardUID = cardUID.Replace("-", string.Empty).ToUpper();

                retSt = calcDevNo(cardUID);
                if (cardUID == null)
                {
                    DisplayStatus();
                }
                else
                {
                    DisplayStatus($"Card-UID: {retSt.value}"); //displaying on text block
                }
                SendKeys.SendWait(retSt.value + "{ENTER}");
            }
        }
        public static retSts calcDevNo(string csn)
        {
            retSts retSt = new retSts();
            retSt.sts = false;
            retSt.value = "Error";
            try
            {
                string rotateCsn = csn.Substring(6, 2) + csn.Substring(4, 2) + csn.Substring(2, 2) + csn.Substring(0, 2);
                long decValue = Convert.ToInt64(rotateCsn, 16);
                retSt.value = decValue.ToString();
                retSt.sts = true;
            }
            catch (Exception e)
            {
                retSt.value = e.ToString();
            }
            return retSt;
        }

        public struct retSts
        {
            public bool sts;
            public string value;
        }

        private bool ConnectCard1()
        {
            if (!CardIo.ConnectCard())
            {
                if (Dispatcher.CheckAccess())
                {
                    textBlockStatus.Text = CardIo.StatusText;
                }
                else
                {
                    Dispatcher.Invoke(() => textBlockStatus.Text = CardIo.StatusText);
                }
                return false;
            }
            return true;
        }

        private enum TextFormat { Hex, Normal, Stretched }

        

        
       
        private void DisplayStatus(string statusText = null, string subStatusText = null)
        {
            textBlockStatus.Text = statusText ?? CardIo.StatusText;
        }


        private void CardIo_ReaderStateChanged(CardIo.ReaderState readerState)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => CardIo_ReaderStateChanged(readerState));
                return;
            }
        }
    }
}