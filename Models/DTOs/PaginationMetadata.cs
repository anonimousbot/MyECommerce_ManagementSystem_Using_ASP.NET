namespace EMS.Models.DTOs
{
    public class PaginationMetadata
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages => TotalItems <= 0 ? 1 : (int)Math.Ceiling(TotalItems / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    public static class PaginationHelper
    {
        public const int DefaultPageNumber = 1;
        public const int DefaultPageSize = 10;
        public const int MaxPageSize = 50;

        public static (int pageNumber, int pageSize) Normalize(int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
            {
                pageNumber = DefaultPageNumber;
            }

            if (pageSize < 1)
            {
                pageSize = DefaultPageSize;
            }
            else if (pageSize > MaxPageSize)
            {
                pageSize = MaxPageSize;
            }

            return (pageNumber, pageSize);
        }

        public static PaginationMetadata Create(int pageNumber, int pageSize, int totalItems)
        {
            return new PaginationMetadata
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems
            };
        }
    }
}
