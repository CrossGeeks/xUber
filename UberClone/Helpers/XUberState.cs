namespace UberClone.Helpers
{
    public enum XUberState
    {
        Initial,
        SearchingOrigin,
        SearchingDestination,
        CalculatingRoute,
        ChoosingRide,
        ConfirmingPickUp,
        ShowingXUberPass,
        ShowingHealthyMeasures,
        AssigningDriver,
        WaitingForDriver,
        TripInProgress,
        TripCompleted
    }
}
