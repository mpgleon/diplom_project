namespace diplom_project.Services
{
    public interface IRatingService
    {
        Task UpdateUserRatingAsync(int userId);
        Task UpdateListingRatingAsync(int listingId);
    }
}
