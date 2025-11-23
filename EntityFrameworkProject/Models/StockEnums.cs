namespace EntityFrameworkProject.Models
{
    public enum StockMovementType : short
    {
        StockReceived = 1,
        StockReserved = 2,
        StockReleased = 3,
        StockShipped = 4,
        StockAdjusted = 5,
        StockTransferredOut = 6,
        StockTransferredIn = 7
    }

    public enum StockReservationStatus : short
    {
        Pending = 0,
        Active = 1,
        Cancelled = 2,
        Consumed = 3
    }
}
