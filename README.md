# WorldLine TIM .NET Demo

A .NET 10 console proof-of-concept that runs a card-payment **happy flow** against a
WorldLine **TIM** payment terminal using the bundled TIM SDK (`TIM-Dotnet-SDK/`).

The app performs a single purchase end-to-end in **CZK**:

```text
Activate -> Transaction(Purchase) -> Commit -> wait for Idle -> Deactivate
```

## Prerequisites

- .NET 10 SDK
- A reachable TIM terminal. Connection settings live in
  [WorldlineTimDemo/TimApi.cfg](WorldlineTimDemo/TimApi.cfg) and default to the test
  terminal at `192.168.0.21:7784` (fixed-IP / `OnFixIP` mode). Edit that file to point
  at a different terminal or change the terminal id.

## Build

```bash
dotnet build WorldlineTimDemo/WorldlineTimDemo.csproj
```

The project references the SDK's `.NET Standard 2.0` assembly directly
(`TIM-Dotnet-SDK/TimApi/Standard/TimApi.dll`), so it works cross-platform.

## Run

```bash
dotnet run --project WorldlineTimDemo
```

You will be prompted for an amount in CZK. During the transaction step the terminal
asks the cardholder to **present the card and enter the PIN** — do this on the physical
terminal. On success the app prints the approved amount, card brand, and the TIM
transaction reference number.

A `TimApi_YYYYMMDD.log` file is written next to the executable
(`WorldlineTimDemo/bin/Debug/net10.0/`) for troubleshooting.

## Scope

This PoC is intentionally limited to the purchase happy flow. Reversal, balance,
reconciliation, and error-recovery flows are out of scope.
