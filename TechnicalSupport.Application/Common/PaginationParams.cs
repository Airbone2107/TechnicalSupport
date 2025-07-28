namespace TechnicalSupport.Application.Common
{
    public class PaginationParams
    {
        private const int MaxPageSize = 50;
        private int _pageNumber = 1;
        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = (value > 0) ? value : 1;
        }

        private int _pageSize = 10;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > 0 && value <= MaxPageSize) ? value : 10;
        }
    }
}