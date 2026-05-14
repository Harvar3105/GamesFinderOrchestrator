namespace GamesFinder.Orchestrator.Domain.Enums;

public enum ECurrency
{
  EUR,
  USD
}

public static class ECurrencyHelpers {
  public static ECurrency? GetECurrency(string currency)
  {
    switch (currency)
    {
      case "EUR":
      case "eur":
      case "Eur":
        return ECurrency.EUR;
      case "USD":
      case "Usd":
      case "usd":
        return ECurrency.USD;
      default:
        return null;
    }
  }
}
