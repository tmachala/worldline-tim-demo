using System.Globalization;
using SIX.TimApi;
using SIX.TimApi.Constants;

namespace WorldlineTimDemo;

/// <summary>
/// Proof-of-concept integration with a WorldLine TIM payment terminal.
/// Drives a single card-purchase happy flow (in CZK) using the synchronous TIM API:
/// Activate -> Transaction(Purchase) -> Commit -> wait for Idle -> Deactivate.
///1
/// Run with the "reconfig" argument to instead perform an EP2 acquirer init
/// (Reconfig), which makes the terminal pull its merchant/acquirer contract from
/// the service center. Use this when a payment fails with "Neplatný obchodník"
/// (invalid merchant) - the host is rejecting the terminal's merchant config.
/// </summary>
internal static class Program
{
    private const string Currency = "CZK";

    private static int Main(string[] args)
    {
        bool reconfigMode = args.Any(a =>
            a.Equals("reconfig", StringComparison.OrdinalIgnoreCase) ||
            a.Equals("--reconfig", StringComparison.OrdinalIgnoreCase));

        return reconfigMode ? RunReconfig() : RunPayment();
    }

    private static int RunPayment()
    {
        Console.WriteLine("WorldLine TIM .NET Demo - card payment happy flow");
        Console.WriteLine("=================================================");

        if (!TryReadAmount(out decimal value))
        {
            Console.WriteLine("No valid amount entered. Aborting.");
            return 1;
        }

        TerminalSettings settings = BuildSettings();

        Amount amount = new Amount(value, Currency);
        Console.WriteLine($"\nStarting purchase of {amount} on terminal {settings.ConnectionIPString}...\n");

        Terminal terminal = new Terminal(settings);

        // POS / user identification for this checkout.
        terminal.PosId = "1";
        terminal.UserId = 1;

        try
        {
            Step("Activating terminal session");
            terminal.Activate();

            Step("Running purchase transaction - present the card and enter PIN on the terminal");
            TransactionResponse response = terminal.Transaction(TransactionType.Purchase, amount);

            // AutoCommit is Off, so the transaction must be committed explicitly.
            Step("Committing transaction");
            terminal.Commit();

            Step("Waiting for the terminal to return to idle");
            while (terminal.TerminalStatus.TransactionStatus != TransactionStatus.Idle)
            {
                Thread.Sleep(500);
            }

            Step("Deactivating terminal session");
            terminal.Deactivate();

            PrintResult(response);
            return 0;
        }
        catch (TimException e)
        {
            Console.WriteLine();
            Console.WriteLine($"Transaction failed ({e.ResultCode}): {e.ErrorMessage}");
            return 1;
        }
        finally
        {
            terminal.Dispose();
            Terminal.TimApiDispose();
        }
    }

    private static int RunReconfig()
    {
        Console.WriteLine("WorldLine TIM .NET Demo - EP2 acquirer init (Reconfig)");
        Console.WriteLine("======================================================");
        Console.WriteLine("Forcing the terminal to reload its merchant/acquirer config from the service center.\n");

        TerminalSettings settings = BuildSettings();
        Terminal terminal = new Terminal(settings);

        // POS / user identification, same as the payment flow.
        terminal.PosId = "1";
        terminal.UserId = 1;

        try
        {
            // Reconfig() connects, runs a terminal config + acquirer init against the
            // service center, and returns the init receipt. It does NOT need an open
            // shift, so we deliberately skip Activate()/Transaction() here.
            Step("Running acquirer init (Reconfig)");
            PrintData printData = terminal.Reconfig();

            Console.WriteLine();
            Console.WriteLine("Reconfig completed");
            Console.WriteLine("------------------");
            PrintReceipt("Merchant receipt", printData.MerchantReceipt);
            PrintReceipt("Cardholder receipt", printData.CardholderReceipt);
            Console.WriteLine("\nTerminal config refreshed. Retry the payment.");
            return 0;
        }
        catch (TimException e)
        {
            Console.WriteLine();
            Console.WriteLine($"Reconfig failed ({e.ResultCode}): {e.ErrorMessage}");
            return 1;
        }
        finally
        {
            terminal.Dispose();
            Terminal.TimApiDispose();
        }
    }

    /// <summary>
    /// Builds the terminal settings shared by the payment and reconfig flows.
    /// Loads the [global] section from TimApi.cfg (copied next to the executable),
    /// falling back to in-code connection settings if the config file is missing.
    /// </summary>
    private static TerminalSettings BuildSettings()
    {
        TerminalSettings settings = new TerminalSettings();

        if (string.IsNullOrEmpty(settings.TerminalId))
        {
            Console.WriteLine("TimApi.cfg not loaded - using built-in connection settings.");
            settings.TerminalId = "25697806";
            settings.ConnectionMode = ConnectionMode.ON_FIX_IP;
            settings.ConnectionIPString = "192.168.0.21";
            settings.FetchBrands = true;
            settings.AutoCommit = false;
        }

        // A real online-authorized transaction can keep the terminal busy for a while,
        // during which it stops answering the SDK's keep-alive pings. Left at its default
        // (keep-alive on), the SDK then declares the link dead (ApiConnectionLostTerminal)
        // mid-payment. Disable keep-alive and allow a generous request timeout so a slow
        // host authorization can complete.
        settings.EnableKeepAlive = false;
        settings.SIXmlRequestTimeout = 180;

        return settings;
    }

    private static void PrintReceipt(string title, string? receipt)
    {
        if (!string.IsNullOrWhiteSpace(receipt))
        {
            Console.WriteLine($"\n{title}:");
            Console.WriteLine(receipt);
        }
    }

    private static bool TryReadAmount(out decimal value)
    {
        Console.Write($"Enter amount in {Currency}: ");
        string? input = Console.ReadLine();

        // Accept both '.' and ',' as decimal separators for convenience.
        string normalized = (input ?? string.Empty).Trim().Replace(',', '.');
        return decimal.TryParse(
            normalized,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out value) && value > 0m;
    }

    private static void Step(string message)
    {
        Console.WriteLine($"-> {message}...");
    }

    private static void PrintResult(TransactionResponse response)
    {
        Console.WriteLine();
        Console.WriteLine("Payment approved");
        Console.WriteLine("----------------");
        Console.WriteLine($"Type:       {response.TransactionType}");

        if (response.Amount != null)
        {
            Console.WriteLine($"Amount:     {response.Amount}");
        }

        CardData? card = response.CardData;
        if (card != null)
        {
            if (!string.IsNullOrEmpty(card.BrandName))
            {
                Console.WriteLine($"Brand:      {card.BrandName}");
            }

            if (!string.IsNullOrEmpty(card.CardNumberPrintable))
            {
                Console.WriteLine($"Card:       {card.CardNumberPrintable}");
            }
        }

        TransactionInformation? info = response.TransactionInformation;
        if (info != null && !string.IsNullOrEmpty(info.SixTrxRefNum))
        {
            Console.WriteLine($"Reference:  {info.SixTrxRefNum}");
        }
    }
}
