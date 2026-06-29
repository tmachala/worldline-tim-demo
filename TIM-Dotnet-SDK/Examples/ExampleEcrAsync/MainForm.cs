using System;
using System.Windows.Forms;
using SIX.TimApi;
using SIX.TimApi.Constants;

namespace ExampleEcr
{
    /**
     * <p>Example ECR implementation using TimApi.</p>
     * 
     * <p>Uses asynchronous handling of terminal events to provide a simple ECR
     * allowing to do purchase, balance and reversal of the last purchase.</p>
     */

    public partial class MainForm : Form
    {
        Terminal terminal;
        string reversableTrxRefNum = "";
        RequestTypes? requestInProgress = null;

        public MainForm()
        {
            InitializeComponent();
            updateUIEnabled();

            TerminalSettings settings = new TerminalSettings();
            terminal = new Terminal(settings);

            /**
	         * <p>Listen to terminal events updating the UI in the main thread.</p>
	         * 
	         * <p>This example implementation handles all functions supported by the Example ECR.
	         * You can use multiple event handlers to handle individual events differently if required.
	         * Handlers can be added and removed any time.</p>
	         * 
	         * <p> For this example only
	         * {@link TerminalListener#transactionCompleted()} and {@link TerminalListener#balanceCompleted()}
	         * is used to enable the buttons again when the terminal is ready for another transaction.</p> 
	         */
            terminal.TerminalStatusChanged += new Terminal.TerminalStatusChangedHandler(terminal_TerminalStatusChanged);
            terminal.TransactionCompleted += new Terminal.TransactionCompletedEventHandler(terminal_TransactionCompleted);
            terminal.BalanceCompleted += new Terminal.BalanceCompletedEventHandler(terminal_BalanceCompleted);
            terminal.ConnectCompleted += new Terminal.ConnectCompletedHandler(terminal_ConnectCompleted);
            terminal.Disconnected += new Terminal.DisconnectedHandler(terminal_Disconnected);
            terminal.LoginCompleted += new Terminal.LoginCompletedEventHandler(terminal_LoginCompleted);
            terminal.LogoutCompleted += new Terminal.LogoutCompletedEventHandler(terminal_LogoutCompleted);
            terminal.ActivateCompleted += new Terminal.ActivateCompletedEventHandler(terminal_ActivateCompleted);
            terminal.DeactivateCompleted += new Terminal.DeactivateCompletedEventHandler(terminal_DeactivateCompleted);
            
            terminal.PosId = "25";

            //Fillin the Amount
            decimal amount = 8.5m;
            tbAmount.Text = amount.ToString("F2");
        }

#region Terminal Event Handlers
        void terminal_TerminalStatusChanged(object sender, TerminalStatus trmStatus)
        {
            lbDisplay.Text = "";

            foreach (string s in trmStatus.DisplayContent)
            {
                lbDisplay.Text += s + "\r\n";
            }
        }

        void terminal_TransactionCompleted(object sender,Terminal.TransactionCompletedEventArgs args)
        {
            setRequestInProgress(null);

            // If event contains a null exception the transaction completed successfully.
            // Use data.getTransactionType() to see what kind of transaction finished if you
            // do not track this information yourself already. getTransactionType() is
            // present for your convenience.
            if (args.TimError == null)
            {
                switch (args.TransactionResponse.TransactionType) {
                    case TransactionType.Purchase:
                    case TransactionType.Credit:
                        setReversableTransaction(args.TransactionResponse.TransactionInformation.SixTrxRefNum);
                        break;

                    default:
                        setReversableTransaction("");
                        break;
                }
            }
            else 
            {
                // If event contains an error the transaction failed. Show an error message.
                // The exception contains the error code and additional information if present.
                // The error message is provided in the exception.
                MessageBox.Show(args.TimError.ErrorMessage, "Transaction failed");
			}
        }    
        
        void terminal_BalanceCompleted(object sender, Terminal.BalanceCompletedEventArgs balanceCompletedEventArgs)
        {
            //logger.info("Balance completed");
			setRequestInProgress(null);
			
			if(balanceCompletedEventArgs.Error != null)
            {
                MessageBox.Show(balanceCompletedEventArgs.TimError.ErrorMessage, "Balance failed");
            }
        }

        void terminal_ConnectCompleted(object sender, TimException exception)
        {
            if(requestInProgress == RequestTypes.Connect)
            {
                setRequestInProgress(null);
                if (exception != null)
                {
                    MessageBox.Show(exception.ErrorMessage, "Connect failed");
                }
            }
        }

        void terminal_Disconnected(object sender, TimException exception)
        {
            setRequestInProgress(null);
        }

        void terminal_LoginCompleted(object sender, Terminal.LoginCompletedEventArgs args)
        {
            if (requestInProgress == RequestTypes.Login)
            {
                setRequestInProgress(null);
                if (args.TimError != null)
                {
                    MessageBox.Show(args.TimError.ErrorMessage, "Login failed");
                }
            }
        }

        void terminal_LogoutCompleted(object sender, Terminal.LogoutCompletedEventArgs args)
        {
            if (requestInProgress == RequestTypes.Logout)
            {
                setRequestInProgress(null);
                if (args.TimError != null)
                {
                    MessageBox.Show(args.TimError.ErrorMessage, "Logout failed");
                }
            }
        }

        void terminal_ActivateCompleted(object sender, Terminal.ActivateCompletedEventArgs args)
        {
            if (requestInProgress == RequestTypes.Activate)
            {
                setRequestInProgress(null);
                if (args.TimError != null)
                {
                    MessageBox.Show(args.TimError.ErrorMessage, "Activate failed");
                }
            }
        }

        void terminal_DeactivateCompleted(object sender, Terminal.DeactivateCompletedEventArgs args)
        {
            if (requestInProgress == RequestTypes.Deactivate)
            {
                setRequestInProgress(null);
                if (args.TimError != null)
                {
                    MessageBox.Show(args.TimError.ErrorMessage, "Deactivate failed");
                }
            }
        }
        #endregion

        /**
	     * Set transaction reference number of last transaction that can be reversed.
	     * Set to null after reversal.
	     */
        void setReversableTransaction(string transactionReferenceNumber)
        {
            reversableTrxRefNum = transactionReferenceNumber;
            updateUIEnabled();
        }

        /**
	    * Do a purchase transaction.
	    */
        private void onPurchase()
        {
            try
            {
                //logger.info("Begin purchase");
                setRequestInProgress(RequestTypes.Transaction);

                // Clear the transaction data. It is used for reversal but not purchase
                terminal.TransactionData = null;

                // Run the transaction. Once completed the event transactionCompleted is raised.
                terminal.TransactionAsync(TransactionType.Purchase,
                    new Amount(Convert.ToDecimal(tbAmount.Text), cbCurrency.Text));
            }
            catch (TimException exc)
            {
                setRequestInProgress(null);
                MessageBox.Show(exc.ErrorMessage, "Transaction failed");
            }
        }

        /**
	    * Do a credit transaction.
	    */
        private void onCredit()
        {
            try
            {
                //logger.info("Begin purchase");
                setRequestInProgress(RequestTypes.Transaction);

                // Clear the transaction data. It is used for reversal but not purchase
                terminal.TransactionData = null;

                // Run the transaction. Once completed the event transactionCompleted is raised.
                terminal.TransactionAsync(TransactionType.Credit,
                    new Amount(Convert.ToDecimal(tbAmount.Text), cbCurrency.Text));
            }
            catch (TimException exc)
            {
                setRequestInProgress(null);
                MessageBox.Show(exc.ErrorMessage, "Credit failed");
            }
        }

        /**
	     * Cancel running transaction.
	     */
        private void onCancel()
        {
            try
            {
                //logger.info("Cancel");
                terminal.Cancel();
            }
            catch (TimException e)
            {
                MessageBox.Show(e.ErrorMessage, "Cancel failed");
            }
        }

        /**
	     * Do a reversal transaction.
	     */
        private void onReversal()
        {
            try
            {
                //logger.info("Begin reversal");
                setRequestInProgress(RequestTypes.Transaction);

                // Set the transaction data. Reversal needs the transaction reference number
                // of the previous transaction
                TransactionData trxData = new TransactionData();
                trxData.SixTrxRefNum = reversableTrxRefNum;
                terminal.TransactionData = trxData;

                // Run the transaction. Once completed the listener receives a
                // transactionCompleted notification
                terminal.TransactionAsync(TransactionType.Reversal,
                    new Amount(Convert.ToDecimal(tbAmount.Text), cbCurrency.Text));

            }
            catch (TimException e)
            {
                setRequestInProgress(null);
                MessageBox.Show(e.ErrorMessage, "Reversal failed");
            }
        }

        /**
	     * Do a balance.
	     */
        private void onBalance()
        {
            try
            {
                setRequestInProgress(RequestTypes.Balance);
                terminal.BalanceAsync();
            }
            catch (TimException e)
            {
                setRequestInProgress(null);
                MessageBox.Show(e.ErrorMessage, "Balance failed");
            }
        }

        /**
	     * Do a connect.
	     */
        private void onConnect()
        {
            try
            {
                setRequestInProgress(RequestTypes.Connect);
                terminal.ConnectAsync();
            }
            catch (TimException e)
            {
                setRequestInProgress(null);
                MessageBox.Show(e.ErrorMessage, "Connect failed");
            }
        }

        /**
	     * Do a disconnect.
	     */
        private void onDisconnect()
        {
            try
            {
                setRequestInProgress(null);
                terminal.DisconnectAsync();
            }
            catch (TimException e)
            {
                setRequestInProgress(null);
                MessageBox.Show(e.ErrorMessage, "Disconnect failed");
            }
        }

        /**
	     * Do a login.
	     */
        private void onLogin()
        {
            try
            {
                setRequestInProgress(RequestTypes.Login);
                terminal.LoginAsync();
            }
            catch (TimException e)
            {
                setRequestInProgress(null);
                MessageBox.Show(e.ErrorMessage, "Login failed");
            }
        }

        /**
	     * Do a logout.
	     */
        private void onLogout()
        {
            try
            {
                setRequestInProgress(RequestTypes.Logout);
                terminal.LogoutAsync();
            }
            catch (TimException e)
            {
                setRequestInProgress(null);
                MessageBox.Show(e.ErrorMessage, "Logout failed");
            }
        }

        /**
	     * Do an activate.
	     */
        private void onActivate()
        {
            try
            {
                setRequestInProgress(RequestTypes.Activate);
                terminal.ActivateAsync();
            }
            catch (TimException e)
            {
                setRequestInProgress(null);
                MessageBox.Show(e.ErrorMessage, "Activate failed");
            }
        }

        /**
	     * Do a deactivate.
	     */
        private void onDeactivate()
        {
            try
            {
                setRequestInProgress(RequestTypes.Deactivate);
                terminal.DeactivateAsync();
            }
            catch (TimException e)
            {
                setRequestInProgress(null);
                MessageBox.Show(e.ErrorMessage, "Deactivate failed");
            }
        }

        /**
	     * Set if a transaction or balance request is running. If true purchase, balance
	     * and reversal buttons are disabled and cancel button enabled.
	     */
        private void setRequestInProgress(RequestTypes? inProgress)
        {
            requestInProgress = inProgress;
            updateUIEnabled();
        }

        private void updateUIEnabled()
        {
            btnConnect.Enabled = requestInProgress == null;
            btnDisconnect.Enabled = requestInProgress == null;
            btnLogin.Enabled = requestInProgress == null;
            btnLogout.Enabled = requestInProgress == null;
            btnActivate.Enabled = requestInProgress == null;
            btnDeactivate.Enabled = requestInProgress == null;
            btnPurchase.Enabled = requestInProgress == null;
            btnCredit.Enabled = requestInProgress == null;
            btnReversal.Enabled = requestInProgress == null && reversableTrxRefNum != "";
            btnBalance.Enabled = requestInProgress == null;
            btnCancel.Enabled = requestInProgress != null;
        }

        #region ButtonEvents
        private void btnPurchase_Click(object sender, EventArgs e)
        {
            onPurchase();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            onCancel();
        }

        private void btnBalance_Click(object sender, EventArgs e)
        {
            onBalance();
        }

        private void btnReversal_Click(object sender, EventArgs e)
        {
            onReversal();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            onConnect();
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            onDisconnect();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            onLogin();
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            onLogout();
        }

        private void btnActivate_Click(object sender, EventArgs e)
        {
            onActivate();
        }

        private void btnDeactivate_Click(object sender, EventArgs e)
        {
            onDeactivate();
        }

        private void btnCredit_Click(object sender, EventArgs e)
        {
            onCredit();
        }
        #endregion

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            terminal.Dispose();
            Terminal.TimApiDispose();
        }
    }
}
