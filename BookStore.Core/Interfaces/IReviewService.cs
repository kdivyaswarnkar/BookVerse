using BookStore.Core.DTOs;

namespace BookStore.Core.Interfaces;

public interface IReviewService
{
    Task<BookReviewsDto> ListAsync(int bookId, int page, int pageSize);
    Task<ReviewDto> SaveAsync(int userId, int bookId, SaveReviewRequest request);   // add or edit my review
    Task DeleteMineAsync(int userId, int bookId);
    Task DeleteAsAdminAsync(int reviewId);
}
