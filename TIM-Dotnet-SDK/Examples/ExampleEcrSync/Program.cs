using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIX.TimApi;

namespace ExampleEcrSync
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create terminal settings to connect to the terminal. Load global ini-section
		    // from TimApi.cfg
            TerminalSettings settings = new TerminalSettings();

            // In case somebody does not use the timapi.cfg as he should fill in the values ourself
            if (settings.TerminalId == "") {
                settings.TerminalId = "12345678";
                settings.ConnectionMode = SIX.TimApi.Constants.ConnectionMode.BROADCAST;
                settings.ConnectionIPString = "localhost";
                settings.FetchBrands = true;
                settings.AutoCommit = false;
            }

            // Create terminal instance.
            Terminal terminal = new Terminal(settings);

            // Set properties affecting the next login and transaction process. Changing POS-ID
            // has no effect until the next logout-login. Changing User-ID affects the next
            // transaction initiated
            terminal.PosId = "25";
            terminal.UserId = 8;

            try
            {
                Console.WriteLine("ExampleEcrSync Demo Program");
                Console.WriteLine("Activate...");
			    // Activate terminal session
			    terminal.Activate();
                Console.WriteLine("Activate completed");
			
                Console.WriteLine("Transaction...");
			    // Do a purchase transaction
			    terminal.Transaction(SIX.TimApi.Constants.TransactionType.Purchase, new Amount((decimal)12.50, "CHF"));
                Console.WriteLine("Transaction completed");
			
                Console.WriteLine("Commit...");
			    // Commit transaction
			    terminal.Commit();
                Console.WriteLine("Commit completed");

                Console.WriteLine("Wait for Idle...");
                while (terminal.TerminalStatus.TransactionStatus != SIX.TimApi.Constants.TransactionStatus.Idle) {
                    System.Threading.Thread.Sleep(500); // sleep for 500ms
                }
                
                Console.WriteLine("Deactivate...");
                // Deactivate terminal session
			    terminal.Deactivate();
                Console.WriteLine("Deactivate completed");

                Console.WriteLine("Balance...");
			    // Request balance
			    terminal.Balance();
                Console.WriteLine("Balance completed");			
		    }
            catch(TimException e)
            {
			    // Request failed. Use e.getRequestType() to learn which request failed.
			    // e.ErrorMessage contains the error message to display
                Console.WriteLine(e.ErrorMessage);
		    }
            Console.WriteLine("Press any key to exit");
            Console.Read();

            // Dispose of terminal. Automatically disconnects if connected. Always call dispose
            terminal.Dispose();
            Terminal.TimApiDispose();
        }
    }
}
