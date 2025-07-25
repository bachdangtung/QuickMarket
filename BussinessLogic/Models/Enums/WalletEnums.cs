namespace BussinessLogic.Models.Enums
{
    public enum TransactionType
    {
        Deposit,
        Withdrawal,
        Purchase,
        Sale,
        Refund
    }

    public enum TransactionStatus
    {
        Pending,
        Completed,
        Failed,
        Canceled
    }

    public enum PaymentMethod
    {
        VPPay,
        BankTransfer,
        CreditCard,
        Cash,
        InternalWallet
    }

    public enum PaymentCurrency
    {
        VND,
        USD
    }
}
